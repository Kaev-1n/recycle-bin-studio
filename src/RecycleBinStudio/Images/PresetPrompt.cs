namespace RecycleBinStudio.Images;

internal sealed record PromptTemplate(string Name, string Description, string Subject);

internal static class PresetPrompt
{
    public static IReadOnlyList<PromptTemplate> Templates { get; } =
    [
        new("Корзина с вашим фото", "Знакомая форма корзины с вашим фото. Слева пусто, справа видна бумага.", """
            Draw a stylized Windows-style recycle bin with an open top, a clear rim, and a slightly tapered body. Turn the main subject of the reference into a recognizable simplified portrait or emblem on the front of the bin. Keep it visible and identical in both versions.
            LEFT = EMPTY. The inside of the bin is visible and contains nothing.
            RIGHT = FULL. Add a few large crumpled sheets protruding above the same rim. Keep the bin, portrait, and perspective identical.
            """),
        new("Реакция персонажа", "Две реакции героя в духе Pop Cat. На иконках сам герой.", """
            Turn the main subject into a standalone cutout icon: a head, silhouette, or object. Do not add a trash can, recycling symbol, or container around the subject.
            LEFT = EMPTY. For a subject with a face, show an unmistakably open mouth as if waiting to be fed.
            RIGHT = FULL. Show the same subject with mouth closed and a satisfied expression.
            For a subject without a face, choose one similarly obvious visual transition native to that subject, such as open to closed or inactive to active. Keep the subject recognizable and make both states easy to tell apart at a glance.
            """)
    ];

    public static string Build(PromptTemplate template) => $$"""
        Use the attached picture as a visual reference for its main subject, whether it is a person, animal, object, or illustration. Generate ONE finished image containing two versions of an icon for Windows Recycle Bin.

        {{template.Subject}}

        Preserve recognizable features. Simplify fine details for readability at 32 × 32 pixels. Match viewpoints, visual scale, lighting, and palette. Signal the state with shape or visible contents, never color alone.

        Layout for automatic splitting
        - Use a wide 2:1 canvas, ideally 2048 × 1024 pixels. If an exact ratio is unavailable, use the nearest wide format.
        - Place the two icons side by side in equal-width halves. Center the left icon at 25% of the canvas width and the right icon at 75%. Center both vertically.
        - Give each icon about 75–80% of the height and at most 80% of its half's width. Leave clear space around both silhouettes and keep the vertical center clear. Everything belonging to an icon must stay fully inside its own half.
        - Use a genuinely transparent background if available. Otherwise use the same flat, light neutral background across the entire canvas. Do not draw a checkerboard or simulate transparency.
        - No text, labels, borders, dividers, cast shadows, decorative elements, or app mockups.

        Output one PNG if possible. If the service only supports another image format, still produce the same single image with this layout.
        """;
}
