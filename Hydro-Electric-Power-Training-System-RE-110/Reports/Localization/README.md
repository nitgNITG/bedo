# Wind experiment English / Arabic localization

## What is configured

- Lean Localization supplies English and Arabic translations from `Assets/_Project/Resources/WindLocalization/Catalogue.json`.
- Arabic instructions, objectives, notices, and quiz text come from the four supplied storyboards. Additional interface and equipment tooltip text is translated in the catalogue; each entry records its source.
- The runtime adds an EN/AR control directly above Reload in both PV Lab and Main Menu, using the existing Reload control visuals, hover states, and UI audio. The language choice persists through Lean Localization's saved preference and scene changes.
- Existing display text, dynamically populated lists, tooltips, graph text, and inactive panels are localized when shown. Editable user input is excluded; input placeholders are translated. Switching to English restores the original text, font, alignment, and card-tab geometry.
- Arabic uses an embedded Segoe UI font, joined letter forms, right-to-left ordering, and wrapping that preserves Latin terminal codes and numbers.
- Experiment identifiers, storyboard events, wiring, cameras, numerical models, recorded values, and export data are not translated or rewritten.

## Remaining artwork boundary

English words printed inside monitor diagram images, equipment textures, or PDF page content are not editable UI text. Those assets remain in their original language; complete Arabic artwork/PDF versions are still required to localize those pixels. This implementation does not paint over scientific diagrams or alter equipment connections.

## Translation maintenance

Add a catalogue entry with `english`, `arabic`, and `source` when adding new interface wording. English keys ignore case, punctuation, whitespace, and rich-text markup. Do not use the catalogue to translate terminal codes, units, scene names, or experiment IDs. The Arabic font and its source font are referenced through `Settings.asset` so they are included in builds.

## Verification

Runtime regression exercises Main Menu -> each EXP1-4 in Guided Mode -> Free Mode, language persistence, EN/AR button callbacks, and restoration to English. Translation coverage includes the inventoried text in both scenes and equipment tooltips. Coverage checks confirm translations for all 75 EXP1â€“4 step-list entries and all 78 inventoried tooltip strings. Runtime checks and tooling are saved alongside this report after testing; temporary Editor scripts are removed from Assets.

The feature has been tested in the Unity Editor. No new Windows player build is claimed here.

Final Editor run: all four Main Menu launches, Guided -> Free transitions, language-control callbacks, language persistence, and English restoration passed. Active Arabic text checked during these transitions reported zero overflow. See `runtime-checks.txt`. Both scene files and existing production scripts remain unchanged.

An additional focused check passed for all 18 interaction warnings: Arabic translation and exact English restoration. The final catalogue contains 392 unique Lean keys.

## UI polish verification

TextMeshPro mesh-only events no longer retrigger translation or font fitting. Text changes and genuine available-width changes still update localization; preferred-width fitters use their containing layout width to avoid wrapping feedback.

The language control copies the right-panel Reload visuals, replaces its click event with language switching, and sits 87 canvas units directly above Reload. Both scenes verified the same position. Frame sampling found zero changes in text, font size, rectangle, or line count across 180 updates in Main Menu and 180 in EXP2/PV Lab. Language switching passed and inherited Reload callback count was zero. See `polish-checks.txt`.

## Vertical card labels and language indicator

Arabic Documents/Objectives labels now use a single joined word on an independent, rotated text area within each tab. The authored English title and its layout remain intact and restore when English is selected. Tab selection/click behaviour is unchanged. The language control displays EN for English and AR for Arabic. Main Menu and PV Lab checks passed for all eight current experiment tab labels, plus language toggling and English restoration. See `vertical-label-checks.txt` and `vertical-labels-pv.png`.

## Arabic popup and quiz positioning

Pre-shaped Arabic glyphs were receiving OpenType positioning again, producing backward glyph placements. Arabic TMP output now disables those extra font features, while each component restores its original feature list in English. Glyph checks found two backward placements in the wind title and one in the View Turbine label before the fix; both now have zero. Current EXP1-4 Arabic quiz text components were checked for the same presentation setting, and popup/quiz screenshots were visually reviewed. The three Wind captions (Normal, Highlighted, Pressed) are disabled in PV Lab; icon geometry, slider, and button events remain unchanged. See `arabic-positioning-checks.json` and the after screenshots.
