using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace streamingDemo_FE.Components
{
    public partial class ChatStream
    {
        [Parameter] public string? Endpoint { get; set; } // lo passi come attributo del custom element
        private IJSObjectReference? _module;
        private IJSObjectReference? _sseHandle;
        private DotNetObjectReference<ChatStream>? _selfRef;

        private string Prompt { get; set; } = "";
        private string Output { get; set; } = "";
        private bool isStreaming = false;

        private List<string> _sources = new();
        private string _tipsTitle = string.Empty;
        private List<string> _tipsItems = new();

        protected override async Task OnInitializedAsync()
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/sse.js");
            _selfRef = DotNetObjectReference.Create(this);
        }

        public async Task Start()
        {
            if (string.IsNullOrWhiteSpace(Endpoint)) return;

            // reset
            Output = "";
            _sources.Clear();
            _tipsTitle = string.Empty;
            _tipsItems.Clear();
            isStreaming = true;

            var url = $"{Endpoint}?prompt={Uri.EscapeDataString(Prompt)}";
            _sseHandle = await _module!.InvokeAsync<IJSObjectReference>("startSse", url, _selfRef);

            Prompt = string.Empty;
            StateHasChanged();
        }

        public async Task Stop()
        {
            if (_sseHandle != null)
            {
                await _sseHandle.InvokeVoidAsync("close");
                _sseHandle = null;
            }
            isStreaming = false;
        }

        [JSInvokable]
        public Task OnChunk(string chunk)
        {
            Output += chunk;
            StateHasChanged(); // aggiorna UI
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnSources(string[] items)
        {
            _sources = items.ToList();
            StateHasChanged();
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnTipsTitle(string chunk)
        {
            _tipsTitle += chunk;
            StateHasChanged();
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnTips(string[] items)
        {
            _tipsItems = items.ToList();
            StateHasChanged();
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnCompleted()
        {
            isStreaming = false;
            StateHasChanged();
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnError(string message)
        {
            isStreaming = false;
            Output += $"\n[errore] {message}";
            StateHasChanged();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _ = Stop();
            _selfRef?.Dispose();
        }
    }
}
