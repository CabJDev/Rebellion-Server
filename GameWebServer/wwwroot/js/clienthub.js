"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ClientHub").build();

connection.on("InvalidLobby", () => {
    document.getElementById("error").innerText = "Invalid Code!"
})

document.getElementById("submitButton").disabled = true;

connection.start().then(function () {
    document.getElementById("submitButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
})

document.getElementById("submitButton").addEventListener("click", function (event) {
    var name = document.getElementById("name").value;
    var lobby = document.getElementById("lobbyCode").value;
    console.log("Test");
    connection.invoke("JoinLobby", name, lobby).catch(function (err) {
        return console.error(err.toString());
    })

    event.preventDefault();
})