const WebSocket = require('ws');
const PORT = process.env.PORT || 3000;
const wss = new WebSocket.Server({ port: PORT });

// Bancos de dados em memória temporária (resetam se o servidor reiniciar)
const users = {}; // { "usuario": "senha" }
const rooms = {}; // { "sala1": [ws1, ws2] }

wss.on('connection', (ws) => {
    ws.currentRoom = null;
    ws.username = null;

    ws.on('message', (message) => {
        try {
            const data = JSON.parse(message);

            // --- REGISTRO / LOGIN ---
            if (data.action === 'register') {
                if (users[data.username]) {
                    ws.send(JSON.stringify({ action: 'register_result', success: false, message: 'Usuário já existe!' }));
                } else {
                    users[data.username] = data.password;
                    ws.send(JSON.stringify({ action: 'register_result', success: true, message: 'Conta criada!' }));
                }
            }

            if (data.action === 'login') {
                if (users[data.username] && users[data.username] === data.password) {
                    ws.username = data.username;
                    ws.send(JSON.stringify({ action: 'login_result', success: true, username: ws.username }));
                } else {
                    ws.send(JSON.stringify({ action: 'login_result', success: false, message: 'Dados incorretos!' }));
                }
            }

            // --- SISTEMA DE SALAS ---
            if (data.action === 'join_room') {
                const roomName = data.room;

                // Cria a sala se não existir
                if (!rooms[roomName]) {
                    rooms[roomName] = [];
                }

                // Adiciona o jogador na sala
                rooms[roomName].push(ws);
                ws.currentRoom = roomName;

                ws.send(JSON.stringify({ action: 'room_joined', room: roomName }));
            }

            // --- MENSAGENS / MOVIMENTO DENTRO DA SALA ---
            if (data.action === 'room_message') {
                if (ws.currentRoom && rooms[ws.currentRoom]) {
                    // Transmite a mensagem apenas para os jogadores da MESMA sala
                    rooms[ws.currentRoom].forEach((client) => {
                        if (client.readyState === WebSocket.OPEN) {
                            client.send(JSON.stringify({
                                action: 'chat',
                                sender: ws.username,
                                text: data.text
                            }));
                        }
                    });
                }
            }

        } catch (e) {
            console.error("Erro ao processar mensagem", e);
        }
    });

    ws.on('close', () => {
        // Remove o jogador da sala quando desconectar
        if (ws.currentRoom && rooms[ws.currentRoom]) {
            rooms[ws.currentRoom] = rooms[ws.currentRoom].filter(client => client !== ws);
        }
    });
});

console.log(`Servidor rodando na porta ${PORT}`);
