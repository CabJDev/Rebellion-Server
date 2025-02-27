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

document.getElementById("submitButton").disabled = true;

connection.start().then(function () {
    document.getElementById("submitButton").disabled = false;
    connection.invoke("UserConnect", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });
}).catch(function (err) {
    return console.error(err.toString());
})

document.getElementById("submitButton").addEventListener("click", function (event) {
    var name = document.getElementById("name").value;
    var lobby = document.getElementById("lobbyCode").value;
    connection.invoke("JoinLobby", name, lobby).catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
})

window.onbeforeunload = function () {
    connection.invoke("UserDisconnect", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });
}