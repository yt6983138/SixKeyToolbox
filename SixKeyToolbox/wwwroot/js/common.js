window.createAudioBlobUrl = function (bytes) {
    const blob = new Blob([bytes], { type: "audio/mpeg" });
    return URL.createObjectURL(blob);
};

window.revokeObjectUrl = function (url) {
    URL.revokeObjectURL(url);
};

window.playAudio = function (audioElementId) {
    const el = document.getElementById(audioElementId);
    if (el) el.play();
};

window.pauseAudio = function (audioElementId) {
    const el = document.getElementById(audioElementId);
    if (el) { el.pause(); }
};

window.getAudioCurrentTime = function (audioElementId) {
    const el = document.getElementById(audioElementId);
    return el ? el.currentTime : 0;
};

window.setAudioCurrentTime = function (audioElementId, seconds) {
    const el = document.getElementById(audioElementId);
    if (el) el.currentTime = seconds;
};

window.getAudioDuration = function (audioElementId) {
    const el = document.getElementById(audioElementId);
    if (!el) return 0;
    const d = el.duration;
    return (typeof d === "number" && isFinite(d)) ? d : 0;
};
