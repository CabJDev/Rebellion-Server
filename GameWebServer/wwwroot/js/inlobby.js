function backButtonPressed() {
    if (connected === false) {
        setTimeout(backButtonPressed, 100);
        return;
    }

    connection.invoke("UserIntentionalDisconnect", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });

    window.location.replace("https://rebelliongame.fun");
}