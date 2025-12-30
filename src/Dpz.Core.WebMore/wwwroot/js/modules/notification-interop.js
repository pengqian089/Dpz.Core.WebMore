export async function requestBrowserNotification(title, content, image = "https://cdn.dpangzi.com/logo.png", tag = "") {
    if (!("Notification" in window)) {
        return false;
    }

    let permission = Notification.permission;
    if (permission !== "granted") {
        permission = await Notification.requestPermission();
    }

    if (permission === "granted") {
        const notification = new Notification(title, {
            body: content,
            icon: image,
            tag: tag,
            lang: "zh-cn"
        });
        
        notification.onclick = function () {
            window.open("/mumble");
            notification.close();
        };
        
        return true;
    }

    return false;
}

