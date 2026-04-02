namespace Prepr.Reporters;

internal static class HtmlReporterStyles
{
    internal const string HEAD = """
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>prepr — Duplicate Block Report</title>
            <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 960px; margin: 0 auto; padding: 2rem; background: #1e1e2e; color: #cdd6f4; }
                h1 { color: #89b4fa; border-bottom: 2px solid #45475a; padding-bottom: 0.5rem; }
                h2 { color: #a6adc8; margin-top: 2rem; }
                .stats { background: #313244; padding: 1rem; border-radius: 8px; margin: 1rem 0; }
                .stats span { margin-right: 2rem; }
                .block { background: #313244; border: 1px solid #45475a; border-radius: 8px; margin: 1.5rem 0; overflow: hidden; }
                .block-header { background: #45475a; padding: 0.75rem 1rem; cursor: pointer; font-weight: bold; color: #f9e2af; }
                .block-header:hover { background: #585b70; }
                .block-body { padding: 1rem; display: none; }
                .block-body.open { display: block; }
                pre { background: #1e1e2e; padding: 1rem; border-radius: 4px; overflow-x: auto; margin: 0.5rem 0; }
                code { color: #a6e3a1; }
                .location { color: #89dceb; margin: 0.25rem 0; }
                table { width: 100%; border-collapse: collapse; margin: 1rem 0; }
                th { background: #45475a; padding: 0.5rem; text-align: left; }
                td { padding: 0.5rem; border-bottom: 1px solid #45475a; }
                .none { color: #a6e3a1; font-style: italic; }
                .total { margin-top: 1rem; font-weight: bold; color: #89b4fa; }
                .severity-high { color: #f38ba8; font-weight: bold; }
                .severity-medium { color: #f9e2af; font-weight: bold; }
                .severity-low { color: #a6e3a1; }
                .pair-detail { margin-left: 2rem; color: #89dceb; font-size: 0.9em; }
            </style>
        </head>
        """;

    internal const string FOOTER = """
        </body>
        </html>
        """;
}
