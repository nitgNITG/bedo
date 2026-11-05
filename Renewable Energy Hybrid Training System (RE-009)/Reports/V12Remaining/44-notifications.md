# Issue 44

Configured all 17 EXP1-4 notification scroll components to position the common notification group above the visible instruction background with a 16-reference-pixel gap. The existing AutoSizeSubtitleScrollContent component performs this optional placement after canvas layout, using the panel bounds and canvas scale. Oversized transparent border/glow rectangles are excluded from the measurement.

All 17 notifications were activated and checked in Play Mode. Each had a 16-pixel gap in reference coordinates; the instruction container position and size stayed unchanged. Placement was visually checked in the running Game View. Notifications keep their authored text, size, scrolling, close controls and activation events. No instruction object, subtitle text, step sequence or quiz was edited.
