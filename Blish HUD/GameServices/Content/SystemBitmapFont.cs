using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.TextureAtlases;

namespace Blish_HUD.Content {
    /// <summary>Rasterizes Windows or private fonts into bounded, disposable atlas pages.</summary>
    internal sealed class SystemBitmapFont : BitmapFont, IDisposable {
        private const int PageSize = 1024;
        private readonly List<Texture2D> _pages;

        private SystemBitmapFont(string name, List<BitmapFontRegion> regions, int lineHeight,
                                 List<Texture2D> pages) : base(name, regions, lineHeight) {
            _pages = pages;
        }

        private static FontFamily OpenFamily(string source, PrivateFontCollection collection) {
            source = source.Trim().Trim('"');
            if (File.Exists(source)) {
                collection.AddFontFile(Path.GetFullPath(source));
                var families = collection.Families;
                if (families.Length == 0) throw new ArgumentException("No font families in file.");
                for (int i = 1; i < families.Length; i++) families[i].Dispose();
                return families[0];
            }
            // FontFamily rejects unknown names instead of silently substituting a font.
            return new FontFamily(source);
        }

        internal static void ValidateSource(string source) {
            if (string.IsNullOrWhiteSpace(source)) return;
            using var collection = new PrivateFontCollection();
            using var family = OpenFamily(source, collection);
            using var font = CreateFont(family, 14, ContentService.FontStyle.Regular);
        }

        private static Font CreateFont(FontFamily family, int size, ContentService.FontStyle style) {
            var drawingStyle = style == ContentService.FontStyle.Bold ? System.Drawing.FontStyle.Bold
                             : style == ContentService.FontStyle.Italic ? System.Drawing.FontStyle.Italic
                             : System.Drawing.FontStyle.Regular;
            if (!family.IsStyleAvailable(drawingStyle)) {
                foreach (var fallback in new[] { System.Drawing.FontStyle.Regular, System.Drawing.FontStyle.Bold,
                                                System.Drawing.FontStyle.Italic,
                                                System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic }) {
                    if (!family.IsStyleAvailable(fallback)) continue;
                    drawingStyle = fallback;
                    break;
                }
            }
            return new Font(family, size, drawingStyle, GraphicsUnit.Pixel);
        }

        private static IEnumerable<char> Characters(bool chinese) {
            for (int c = 0x20; c <= 0x17f; c++) yield return (char)c;
            for (int c = 0x2000; c <= 0x206f; c++) yield return (char)c;
            for (int c = 0x20a0; c <= 0x20cf; c++) yield return (char)c;
            for (int c = 0x2190; c <= 0x25ff; c++) yield return (char)c;
            if (!chinese) yield break;
            for (int c = 0x3000; c <= 0x303f; c++) yield return (char)c;
            for (int c = 0x4e00; c <= 0x9fff; c++) yield return (char)c;
            for (int c = 0xff00; c <= 0xffef; c++) yield return (char)c;
        }

        internal static SystemBitmapFont Create(string source, int size, ContentService.FontStyle style,
                                                bool chinese, GraphicsDevice device) {
            using var collection = new PrivateFontCollection();
            using var family = OpenFamily(source, collection);
            using var font = CreateFont(family, size, style);
            using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;

            int lineHeight = (int)Math.Ceiling(font.GetHeight(96));
            int cellHeight = lineHeight + 4;
            var pages = new List<Texture2D>();
            var regions = new List<BitmapFontRegion>();
            var pending = new List<(char Character, Rectangle Bounds, int Advance)>();
            using var bitmap = new Bitmap(PageSize, PageSize, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(96, 96);
            using var graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.Clear(System.Drawing.Color.Transparent);

            void FlushPage() {
                if (pending.Count == 0) return;
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                var texture = TextureUtil.FromStreamPremultiplied(device, stream);
                if (texture == null || texture == ContentService.Textures.Error)
                    throw new InvalidOperationException("Could not create font atlas texture.");
                pages.Add(texture);
                foreach (var glyph in pending) {
                    var b = glyph.Bounds;
                    regions.Add(new BitmapFontRegion(new TextureRegion2D(texture, b.X, b.Y, b.Width, b.Height),
                                                     glyph.Character, -2, 0, glyph.Advance));
                }
                pending.Clear();
                graphics.Clear(System.Drawing.Color.Transparent);
            }

            try {
                int x = 0, y = 0;
                foreach (char character in Characters(chinese)) {
                    string text = character.ToString();
                    int advance = Math.Max(1, (int)Math.Ceiling(graphics.MeasureString(text, font, PointF.Empty, format).Width));
                    int width = advance + 6;
                    if (width > PageSize || cellHeight > PageSize) throw new ArgumentException("Font glyph is too large.");
                    if (x + width > PageSize) { x = 0; y += cellHeight; }
                    if (y + cellHeight > PageSize) { FlushPage(); x = 0; y = 0; }
                    graphics.DrawString(text, font, Brushes.White, new PointF(x + 2, y), format);
                    pending.Add((character, new Rectangle(x, y, width, cellHeight), advance));
                    x += width;
                }
                FlushPage();
                return new SystemBitmapFont(source, regions, lineHeight, pages);
            } catch {
                foreach (var page in pages) page.Dispose();
                throw;
            }
        }

        public void Dispose() {
            foreach (var page in _pages) page.Dispose();
            _pages.Clear();
        }
    }
}
