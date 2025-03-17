"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("https://rebelliongame.fun/ClientHub").build();
var connected = false;

async function start() {
    try {
        await connection.start();
        console.log("SignalR has been connected!");
        connected = true;
        UserConnected();
    } catch (err) {
        console.log(err);
        setTimeout(start, 5000);
    }
};

setTimeout(function () {
    start();
}, 1000);

function UserConnected() {
    connection.invoke("UserConnect", document.cookie, window.location.href).catch(function (err) {
        return console.error(err.toString());
    });
}

connection.on("Redirect", (page) => {
    if (window.location.href != page)
        window.location.replace(page);
})

connection.on("SetCookie", (cookie) => {
    document.cookie = cookie;
})

window.onbeforeunload = function () {
    while (connected === false) { }
    connection.invoke("UserDisconnect", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });
}