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
            .prompt-modal-backdrop {
                position: fixed; inset: 0; z-index: 50;
                background: rgba(0,0,0,0.7); backdrop-filter: blur(4px);
                display: flex; align-items: center; justify-content: center;
            }
            .prompt-modal {
                background: #0f1930; border: 1px solid #40485d;
                border-radius: 0.5rem; max-width: 48rem; width: 90%;
                max-height: 80vh; display: flex; flex-direction: column;
            }
            .prompt-modal-header {
                display: flex; align-items: center; justify-content: space-between;
                padding: 1rem 1.5rem; border-bottom: 1px solid #40485d;
            }
            .prompt-modal-body {
                padding: 1.5rem; overflow-y: auto; flex: 1;
            }
            .prompt-modal-body pre {
                white-space: pre-wrap; word-break: break-word;
                font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
                font-size: 0.8125rem; line-height: 1.6; color: #dee5ff;
            }
        </style>
        </head>
        """;

    internal const string FOOTER = """
        <div id="promptModal" class="prompt-modal-backdrop" style="display:none" onclick="if(event.target===this)closeModal()">
        <div class="prompt-modal">
        <div class="prompt-modal-header">
        <h3 style="font-family:'Space Grotesk';font-weight:700;font-size:1.125rem;color:#dee5ff">AI Prompt</h3>
        <div style="display:flex;gap:0.5rem">
        <button onclick="copyModalPrompt(this)" class="px-3 py-1.5 rounded text-xs font-bold bg-secondary/10 text-secondary border border-secondary/20 hover:bg-secondary/20 transition-colors" style="cursor:pointer">Copy</button>
        <button onclick="closeModal()" class="px-3 py-1.5 rounded text-xs font-bold bg-outline-variant/20 text-on-surface-variant border border-outline-variant/20 hover:bg-outline-variant/30 transition-colors" style="cursor:pointer">Close</button>
        </div>
        </div>
        <div class="prompt-modal-body"><pre id="promptModalText"></pre></div>
        </div>
        </div>
        <script>
        function showPromptModal(el){var t=el.getAttribute('data-prompt');document.getElementById('promptModalText').textContent=t;document.getElementById('promptModal').style.display='';}
        function closeModal(){document.getElementById('promptModal').style.display='none';}
        function copyModalPrompt(btn){var t=document.getElementById('promptModalText').textContent;navigator.clipboard.writeText(t).then(function(){var o=btn.textContent;btn.textContent='Copied!';setTimeout(function(){btn.textContent=o},1500)})}
        function copyPrompt(el){var t=el.getAttribute('data-prompt');navigator.clipboard.writeText(t).then(function(){var o=el.textContent;el.textContent='Copied!';setTimeout(function(){el.textContent=o},1500)})}
        </script>
        </main>
        </body>
        </html>
        """;
}
