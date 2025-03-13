window.onload = function () {
    Initialize();
}

function Initialize() {
    if (connected === false) {
        setTimeout(Initialize, 100);
        return;
    }

    window.setTimeout(GetRoleInfo, 100);
    window.setTimeout(GetPlayerNames, 100);
    for (var i = 0; i < 15; i++) {
        document.getElementById("player" + (i + 1) + "Name").innerText = "";

        var button = document.getElementById("player" + (i + 1) + "Button")
        button.disabled = true;
        button.style.opacity = "0";

        document.getElementById("p" + (i + 1)).style.opacity = "0";
    }
}

var currentChoice = 0;

// Button events
document.getElementById("player1Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 1) {
        document.getElementById("player1Button").innerText = "X";
        currentChoice = 1;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player2Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 2) {
        document.getElementById("player2Button").innerText = "X";
        currentChoice = 2;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player3Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 3) {
        document.getElementById("player3Button").innerText = "X";
        currentChoice = 3;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player4Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 4) {
        document.getElementById("player4Button").innerText = "X";
        currentChoice = 4;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player5Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 5) {
        document.getElementById("player5Button").innerText = "X";
        currentChoice = 5;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player6Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 6) {
        document.getElementById("player6Button").innerText = "X";
        currentChoice = 6;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player7Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 7) {
        document.getElementById("player7Button").innerText = "X";
        currentChoice = 7;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player8Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 8) {
        document.getElementById("player8Button").innerText = "X";
        currentChoice = 8;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player9Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 9) {
        document.getElementById("player9Button").innerText = "X";
        currentChoice = 9;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player10Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 10) {
        document.getElementById("player10Button").innerText = "X";
        currentChoice = 10;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player11Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 11) {
        document.getElementById("player11Button").innerText = "X";
        currentChoice = 11;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player12Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 12) {
        document.getElementById("player12Button").innerText = "X";
        currentChoice = 12;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player13Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 13) {
        document.getElementById("player13Button").innerText = "X";
        currentChoice = 13;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player14Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 14) {
        document.getElementById("player14Button").innerText = "X";
        currentChoice = 14;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
document.getElementById("player15Button").addEventListener("click", function () {
    ResetButtons()
    if (currentChoice != 15) {
        document.getElementById("player15Button").innerText = "X";
        currentChoice = 15;
    }
    else { currentChoice = 0; }

    SendTarget(currentChoice);
})
// Button events

document.getElementById("messageSendButton").addEventListener("click", function () {
    SendMessage()
})

document.getElementById("messageInput").addEventListener("keyup", ({ key }) => {
    if (key === "Enter") {
        SendMessage()
    }
})

function SendMessage() {
    if (connected === false) {
        document.getElementById("messageInput").value = "";
        return;
    }
    var message = document.getElementById("messageInput").value;
    if (message == "") { return; }

    const outgoingDiv = document.createElement("div");
    outgoingDiv.className = "outgoing";

    const chatbox = document.getElementById("chatBox");
    chatbox.appendChild(outgoingDiv);

    const text = document.createElement("p")
    text.innerText = message;
    outgoingDiv.appendChild(text);

    connection.invoke("SendMessage", document.cookie, message).catch(function (err) {
        return console.error(err.toString());
    })

    document.getElementById("messageInput").value = '';
}

function ResetButtons() {
    for (var i = 0; i < 15; i++) {
        element = document.getElementById("player" + (i + 1) + "Button").innerText = "";
    }
}

function GetRoleInfo() {
    if (connected === false) return;
    connection.invoke("RetrieveRoleInfo", document.cookie, Date.now()).catch(function (err) {
        return console.error(err.toString());
    });
}

function GetPlayerNames() {
    if (connected === false) return;
    connection.invoke("RetrievePlayerNames", document.cookie, Date.now()).catch(function (err) {
        return console.error(err.toString());
    });
}

function SendTarget(selection) {
    if (connected === false) return;
    connection.invoke("PlayerTarget", document.cookie, selection, Date.now()).catch(function (err) {
        return console.error(err.toString());
    });
}

connection.on("PlayerKilled", (playerIndex) => {
    document.getElementById("player" + playerIndex + "Name").style.textDecoration = "line-through";
    document.getElementById("p" + playerIndex).style.opacity = "0.5";
})

connection.on("DisableButtons", () => {
    currentChoice = 0;
    ResetButtons();

    for (var i = 0; i < 15; i++) {
        var button = document.getElementById("player" + (i + 1) + "Button")
        button.disabled = true;
        button.style.opacity = "0";
    }
})

connection.on("GetRoles", (name, role, winCon) => {
    document.getElementById("roleName").innerText = name;
    document.getElementById("roleDesc").innerText = role;
    document.getElementById("winConDesc").innerText = winCon;
})

connection.on("GetNames", (names) => {
    for (var i = 0; i < 15; i++) {
        if (names[i] != "") {
            document.getElementById("player" + (i + 1) + "Name").innerText = names[i];
            document.getElementById("p" + (i + 1)).style.opacity = "1";
        }
    }
})

connection.on("EnableButtons", (toEnable) => {
    for (var i = 0; i < 15; i++) {
        if (toEnable[i] == 1) {
            var button = document.getElementById("player" + (i + 1) + "Button")
            button.disabled = false;
            button.style.opacity = "1";
        }
    }
})

connection.on("ReceivePlayerMessage", (sender, message) => {
    const incomingDiv = document.createElement("div");
    incomingDiv.className = "incoming";

    const chatbox = document.getElementById("chatBox");
    chatbox.appendChild(incomingDiv);

    const header = document.createElement("h3")
    header.innerText = sender + ":";
    incomingDiv.appendChild(header);

    const text = document.createElement("p")
    text.innerText = message;
    incomingDiv.appendChild(text);
})

connection.on("ReceiveSystemMessage", (message, timestamp) => {
    const systemDiv = document.createElement("div");
    systemDiv.className = "system";

    const chatbox = document.getElementById("chatBox");
    chatbox.appendChild(systemDiv);

    const text = document.createElement("p")
    text.innerText = message;
    systemDiv.appendChild(text);

    console.log(Date.now() - Number(timestamp));
})