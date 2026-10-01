# Chinese interface and custom fonts

In **Settings → Overlay Settings**, select **Chinese** under **Application & API
Language**, then restart Blish HUD. This loads the Simplified Chinese (`zh-CN`)
resources and a Windows Chinese font before creating the interface controls.
Switching into Chinese from a session without Chinese glyphs keeps the current
interface language until restart. The language selection is saved normally.

**Custom interface font** accepts either an installed font family (for example,
`Microsoft YaHei`) or a complete path to a TrueType `.ttf` / `.ttc` file. For a
font collection, the first family is used. Click outside the text box to save,
then restart. Clear the field and restart to restore the default. Chinese mode
tries Microsoft YaHei, Microsoft JhengHei, SimSun, and Noto Sans CJK SC in order;
other languages retain the packaged Menomonia font when no custom font is set.

Invalid input is rejected with a notification. If a previously saved font has
been removed or becomes unreadable, startup logs the problem and selects the
default. Choose a font containing Chinese glyphs for the Chinese interface.

Fonts returned by `Content.GetFont` and `Content.DefaultFont*` use this setting.
Modules that load their own fonts remain responsible for their font selection
and translations. The atlas includes the basic CJK unified ideographs
(U+4E00–U+9FFF), Chinese punctuation and full-width forms; supplementary-plane
ideographs and emoji are not covered. Atlas pages are generated lazily per
size/style and retained until shutdown. Chinese fonts require more loading time
and GPU memory than the packaged Latin fonts.

## Validation

Run the portable resource checks with:

```sh
python3 scripts/check_chinese_resources.py
```

On Windows, build the solution in both Debug and Release using the prerequisites
in the README, then check:

1. Start with existing English settings. Select Chinese, confirm the restart
   notification, and restart. Check the settings menus, module management, API
   key management, punctuation, and numbers for missing or clipped glyphs.
2. Launch with fresh settings on a Chinese Windows account. Confirm the first
   interface uses Chinese resources and readable fonts.
3. Select an installed font, then a `.ttf` file whose path contains spaces, and
   a `.ttc` collection. Restart after each change. Check small labels, large
   headings, bold/italic text, tooltips, and multiple UI/DPI scale settings.
4. Enter a nonexistent family, nonexistent file and corrupt font file. Confirm
   the previous value is preserved and an error notification appears. Remove a
   previously selected font file before launch and verify default-font recovery.
5. Clear the font field and restart; verify the default for the selected language.
   Switch back to English and restart; verify the packaged fonts are restored.
6. Enable modules using shared core fonts. Open and close their UI repeatedly;
   check atlas memory stabilizes after each size/style has been used. Exit while
   modules are enabled and check for graphics-resource disposal errors.
