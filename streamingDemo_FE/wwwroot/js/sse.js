// wwwroot/js/sse.js
export function startSse(url, dotNetRef) {
    const es = new EventSource(url, { withCredentials: false });

    es.addEventListener('answer_delta', (e) => {
        try {
            const data = JSON.parse(e.data);
            dotNetRef.invokeMethodAsync('OnChunk', data.content);
        } catch {
            dotNetRef.invokeMethodAsync('OnChunk', e.data);
        }
    });

    es.addEventListener('relevantSources', (e) => {
        try {
            const data = JSON.parse(e.data);
            dotNetRef.invokeMethodAsync('OnSources', data.items);
        } catch {
            console.warn('relevant_sources parse error', e.data);
        }
    });

    es.addEventListener('tips_delta', (e) => {
        try {
            const data = JSON.parse(e.data);
            dotNetRef.invokeMethodAsync('OnTipsTitle', data.content);
        } catch {
            dotNetRef.invokeMethodAsync('OnTipsTitle', e.data);
        }
    });

    es.addEventListener('tips', (e) => {
        try {
            const data = JSON.parse(e.data);
            dotNetRef.invokeMethodAsync('OnTips', data.items);
        } catch {
            console.warn('tips parse error', e.data);
        }
    });

    es.addEventListener('done', () => {
        dotNetRef.invokeMethodAsync('OnCompleted');
        es.close();
    });

    es.addEventListener('error', (e) => {
        try {
            const data = JSON.parse(e.data);
            dotNetRef.invokeMethodAsync('OnError', data.message ?? 'Stream error');
        } catch {
            dotNetRef.invokeMethodAsync('OnError', 'Stream error');
        }
        es.close();
    });


    return {
        close: () => es.close()
    };
}