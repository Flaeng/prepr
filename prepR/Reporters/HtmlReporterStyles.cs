namespace Prepr.Reporters;

internal static class HtmlReporterStyles
{
    internal const string HEAD = """
        <head>
        <meta charset="utf-8"/>
        <meta content="width=device-width, initial-scale=1.0" name="viewport"/>
        <title>prepr report</title>
        <link href="https://fonts.googleapis.com" rel="preconnect"/>
        <link crossorigin="" href="https://fonts.gstatic.com" rel="preconnect"/>
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600&amp;family=Space+Grotesk:wght@500;700&amp;display=swap" rel="stylesheet"/>
        <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&amp;display=swap" rel="stylesheet"/>
        <script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
        <script>
          tailwind.config = {
            darkMode: "class",
            theme: {
              extend: {
                colors: {
                  "background": "#060e20",
                  "primary": "#0ea5e9",
                  "secondary": "#69f6b8",
                  "tertiary": "#ff6f7e",
                  "on-surface": "#dee5ff",
                  "on-surface-variant": "#a3aac4",
                  "surface-container": "#0f1930",
                  "surface-container-high": "#141f38",
                  "surface-container-low": "#091328",
                  "outline-variant": "#40485d",
                  "error": "#ff716c",
                  "error-container": "#9f0519",
                },
                fontFamily: {
                  "headline": ["Space Grotesk"],
                  "body": ["Inter"],
                  "label": ["Inter"],
                  "mono": ["ui-monospace", "SFMono-Regular", "Menlo", "Monaco", "Consolas", "Liberation Mono", "Courier New", "monospace"]
                },
                borderRadius: {"DEFAULT": "0.125rem", "lg": "0.25rem", "xl": "0.5rem", "full": "0.75rem"},
              },
            },
          }
        </script>
        <style>
            body { font-family: 'Inter', sans-serif; }
            .material-symbols-outlined {
                font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
            }
            .code-block {
                background-color: #000000;
            }
            details > summary { list-style: none; }
            details > summary::-webkit-details-marker { display: none; }
        </style>
        </head>
        """;

    internal const string FOOTER = """
        </main>
        </body>
        </html>
        """;
}
