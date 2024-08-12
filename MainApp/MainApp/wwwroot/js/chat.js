const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveMessage", (message) => {
    appendMessage(message);
    // Отправка ответа после получения сообщения от пользователя
    const response = `Received your message: ${message}`;
    appendMessage(response);
});

connection.start().then(() => {
    console.log("SignalR connection established.");
}).catch((err) => {
    console.error(err.toString());
});

function sendMessage() {
    const message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", message)
        .catch((err) => {
            console.error(err.toString());
        });
    document.getElementById("messageInput").value = "";
}

function appendMessage(message) {
    const messagesDiv = document.getElementById("messages");
    const messageDiv = document.createElement("div");
    messageDiv.textContent = message;
    messagesDiv.appendChild(messageDiv);
}
