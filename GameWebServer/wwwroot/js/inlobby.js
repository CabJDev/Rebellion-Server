"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ClientHub").build();

connection.on("ReceiveMessage", (type, message) => {
    document.getElementById("error").innerText = message;
})

connection.on("Redirect", (page) => {
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

function backButtonPressed() {
    connection.invoke("UserDisconnect").catch(function (err) {
        return console.error(err.toString());
    });
}