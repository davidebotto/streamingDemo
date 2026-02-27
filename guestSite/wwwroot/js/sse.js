// wwwroot/js/sse.js
export function startSse(url, dotNetRef) {
    const es = new EventSource(url, { withCredentials: false });

    es.onmessage = (e) => {
        try {
            console.log(e);
            const data = JSON.parse(e.data);
            dotNetRef.invokeMethodAsync('OnChunk', data.content);
        } catch {
            // se non è JSON, passa la stringa grezza
            dotNetRef.invokeMethodAsync('OnChunk', e.data);
        }
    };

    es.addEventListener('end', () => {
        dotNetRef.invokeMethodAsync('OnCompleted');
        es.close();
    });

    es.onerror = (err) => {
        console.log(err);
        dotNetRef.invokeMethodAsync('OnError', 'Stream error');
        es.close();
    };

    return {
        close: () => es.close()
    };
}