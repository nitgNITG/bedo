# Issue 25 source image quality audit

Status: blocked by source image resolution. No viewer, document or experiment behavior changed.

The PDFAsset referenced by both Main Menu and PV Lab is Assets/_Project/Documents/Photovoltaics_Components.asset. Its embedded bytes exactly match Assets/_Project/Documents/Components.pdf (11 pages). Both scenes already override m_MaxZoomFactorTextureQuality to 8, matching the viewer's maximum zoom factor. Raising that cap cannot recreate missing image detail.

Examples of embedded source images: mounting frame 290x436 pixels (page 2); turbine 398x628 (page 3); AC halogen module 407x622 (page 4); AC/DC LED photos 313x455 and 339x499 (page 5). The screenshots in checklist issue 25 show these image limitations. A higher-resolution original PDF or photographs are required to close this item. The source document was not upscaled or replaced with invented detail.

Verification: extracted the actual PDFAsset byte stream, compared bytes against the source PDF, inspected all 11 pages' image dimensions with pypdf, and checked viewer quality overrides in both scenes.
