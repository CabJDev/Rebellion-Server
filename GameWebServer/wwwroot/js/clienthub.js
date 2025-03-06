"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ClientHub").build();
var connected = false;

connection.on("Redirect", (page) => {
    if (window.location.href != page)
        window.location.replace(page);
})

connection.on("SetCookie", (cookie) => {
    document.cookie = cookie;
})

connection.start().then(function () {
    connection.invoke("UserConnect", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });
}).catch(function (err) {
    return console.error(err.toString());
})

window.onbeforeunload = function () {
    connection.invoke("UserDisconnect", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });
}