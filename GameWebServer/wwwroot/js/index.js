﻿connection.on("ReceiveMessage", (type, message) => {
    document.getElementById("error").innerText = message;
})

document.getElementById("submitButton").addEventListener("click", function (event) {
    var name = document.getElementById("name").value;
    var lobby = document.getElementById("lobbyCode").value;
    connection.invoke("JoinLobby", name, lobby).catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
})

