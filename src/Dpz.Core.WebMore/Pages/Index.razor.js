import Artplayer from 'https://dpangzi.com/library/artplayer/artplayer.mjs';
import artplayerPluginDanmuku from 'https://dpangzi.com/library/artplayer-plugin-danmuku/artplayer-plugin-danmuku.mjs'
import Hls from 'https://dpangzi.com/library/hls.js/hls.mjs';

/**
 * Initialize Home Video Player
 * @param {Object} video - The video model from C#
 * @param {string} webApiBaseAddress - The API base address
 */
export function initVideo(video, webApiBaseAddress) {
    const containerSelector = '.js-home-video-container';
    const container = document.querySelector(containerSelector);

    if (!container || !video) {
        return;
    }

    // Ensure no trailing slash
    const baseAddress = webApiBaseAddress.endsWith('/')
        ? webApiBaseAddress.slice(0, -1)
        : webApiBaseAddress;

    const videoId = video['id'];
    let hasPlayed = false;

    const art = new Artplayer({
        container: container,
        url: video['m3u8'],
        type: "m3u8",
        poster: video['cover'],
        fullscreenWeb: true,
        fullscreen: true,
        customType: {
            m3u8: playM3u8
        },
        plugins: [
            artplayerPluginDanmuku({
                danmuku: async function () {
                    let danmakuResponse = await fetch(`${baseAddress}/api/Video/danmaku?id=${videoId}`);
                    const data = await danmakuResponse.json();
                    return data.map(x => ({
                        border: false,
                        color: x["color"],
                        mode: x["position"],
                        text: x["text"],
                        time: x["time"]
                    }));
                },
                async beforeEmit(danmaku) {
                    if (danmaku.text === null || danmaku.text.trim() === "") {
                        return false;
                    }

                    let hex = danmaku.color.trim().replace(/^#/, '');

                    // 支持 #RGB 转换为 #RRGGBB
                    if (hex.length === 3) {
                        hex = hex.split('').map(ch => ch + ch).join('');
                    }

                    // 校验是否为有效的6位16进制
                    if (!/^[0-9A-Fa-f]{6}$/.test(hex)) {
                        throw new Error('无效的16进制颜色格式');
                    }

                    // 转换为整数
                    const color = parseInt(hex, 16);
                    const data = {
                        id: videoId,
                        text: danmaku.text,
                        color: color,
                        time: danmaku.time,
                        type: danmaku.mode
                    };
                    await fetch(`${baseAddress}/api/Video/danmaku`, {
                        method: 'post',
                        headers: {
                            'Content-Type': 'application/json;charset=utf-8'
                        },
                        body: JSON.stringify(data)
                    });
                    return true;

                }
            })
        ],
        controls: [],
    });

    art.on('fullscreenWeb', (state) => {
        if (state) {
            container.classList.add('is-web-fullscreen');
        } else {
            container.classList.remove('is-web-fullscreen');
        }
    });

    art.on('fullscreen', (state) => {
        if (state) {
            container.classList.add('is-fullscreen');
        } else {
            container.classList.remove('is-fullscreen');
        }
    });

    art.on('play', async () => {
        if (!hasPlayed) {
            hasPlayed = true;
            await fetch(`${baseAddress}/api/Video/play/${videoId}`, {method: 'PATCH'});
        }
    });
}

function playM3u8(video, url, art) {
    if (Hls.isSupported()) {
        if (art.hls) art.hls.destroy();
        const hls = new Hls();
        hls.loadSource(url);
        hls.attachMedia(video);
        art.hls = hls;
        art.on('destroy', () => hls.destroy());
    } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = url;
    } else {
        art.notice.show = 'Unsupported playback format: m3u8';
    }
}

