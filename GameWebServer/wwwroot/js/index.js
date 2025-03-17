connection.on("ErrorMessage", (message) => {
    document.getElementById("error").innerText = message;
})

function lobbyCodeLimit(element) {
    element.value = element.value.toUpperCase()
    if (element.value.length > 6) {
        element.value = element.value.substring(0, 6);
    }
}

function nameLimit(element) {
    if (element.value.length > 12) {
        element.value = element.value.substring(0, 12);
    }
}

document.getElementById("submitButton").addEventListener("click", function (event) {
    var name = document.getElementById("name").value;
    var lobby = document.getElementById("lobbyCode").value;

    while (connected === false) { }
    connection.invoke("JoinLobby", name, lobby).catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
})