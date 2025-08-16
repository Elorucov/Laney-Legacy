/* vk.cc/aCRM7i */
window.external.notify(JSON.stringify({ method: "DebugInfo", param: "Script injected!" }));
main();

function main() {
    try {
        let sources = new Array();

        let videoElements = document.getElementsByTagName("video");
        window.external.notify(JSON.stringify({ method: "DebugInfo", param: "videoElements: " + videoElements.length }));

        let videoSources = videoElements[0].getElementsByTagName("source");
        for (var i = 0; i < videoSources.length; i++) {
            let source = videoSources[i];
            if (source.type === "video/mp4") {
                let url = new URL(source.src);
                if (url.host.includes("mycdn.me")) { // Clip
                    sources.push({ src: source.src, resolution: 0 });
                    break;
                } else { // video
                    let res = url.pathname.split(".")[1];
                    sources.push({ src: source.src, resolution: parseInt(res) });
                }
            }
        }
        window.external.notify(JSON.stringify({ method: "VideoLinksGrabbed", param: sources }));
    } catch (err) {
        window.external.notify(JSON.stringify({ method: "JSError", param: { name: err.name, message: err.message, trace: err.trace } }));
    }
}