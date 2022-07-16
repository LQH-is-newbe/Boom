import express from 'express';
import childProcess from 'child_process';
import bodyParser from 'body-parser';

const PORT = 8080;
const HOST = '0.0.0.0';

const app = express();
var jsonParser = bodyParser.json();

const usedPorts = new Set();
const rooms = new Map();
let roomIdIterator = 0;

app.post('/create-room', jsonParser, (req, res) => {
    let port = 7777;
    while (usedPorts.has(port)) ++port;
    usedPorts.add(port);
    let passcode = makeid(5);
    let roomId = roomIdIterator++;
    let room = {
        roomId,
        port,
        passcode,
        message: req.body.message,
        started: false,
        numPlayers: 0,
    };
    rooms.set(roomId, room);
    const command = `docker run -d --name room${roomId} -p ${port}:7777/udp -v "${process.env.GAME}":/home -e ROOM_ID=${roomId} -e PASSCODE=${passcode} --add-host=host.docker.internal:host-gateway game`;
    childProcess.exec(command);
    let resBody = {
        roomId,
        port,
        passcode,
    };
    res.end(JSON.stringify(resBody));
});

app.post('/join-room', jsonParser, (req, res) => {
    const room = rooms.get(req.body.roomId);
    if (room.numPlayers >= 4 || room.started) {
        res.status(403);
        res.end();
        return;
    }
    let resBody = {
        roomId: room.roomId,
        port: room.port,
        passcode: room.passcode,
    };
    res.end(JSON.stringify(resBody));
});

app.post('/get-rooms', jsonParser, (req, res) => {
    const result = Array.from(rooms.values());
    res.end(JSON.stringify(result));
});

// game server calls

app.post('/player-join', jsonParser, (req, res) => {
    ++rooms.get(req.body.roomId).numPlayers;
    res.end();
});

app.post('/player-leave', jsonParser, (req, res) => {
    const room = rooms.get(req.body.roomId)
    if(--room.numPlayers == 0) {
        closeRoom(room.roomId);
    }
    res.end();
});

app.post('/close-room', jsonParser, (req, res) => {
    closeRoom(req.body.roomId);
    res.end();
});

app.post('/start-game', jsonParser, (req, res) => {
    rooms.get(req.body.roomId).started = true;
    res.end();
});

app.post('/end-game', jsonParser, (req, res) => {
    rooms.get(req.body.roomId).started = false;
    res.end();
});

// util functions

function closeRoom(roomId) {
    const room = rooms.get(roomId);
    usedPorts.delete(room.port);
    rooms.delete(room.roomId);
    const hash = childProcess.execSync(`docker ps --filter name=room${room.roomId} -q`);
    childProcess.exec(`docker rm --force ${hash}`);
}

function makeid(length) {
    var result           = '';
    var characters       = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    var charactersLength = characters.length;
    for ( var i = 0; i < length; i++ ) {
      result += characters.charAt(Math.floor(Math.random() * 
 charactersLength));
   }
   return result;
}

app.listen(PORT, HOST);
console.log(`Running on http://${HOST}:${PORT}`);