import express from 'express';
import childProcess from 'child_process';
import bodyParser from 'body-parser';
import gameServerRepository from './entities/game-server.js';
import roomRepository from './entities/room.js';

const PORT = 8080;
const HOST = '0.0.0.0';

const app = express();
var jsonParser = bodyParser.json();

let roomIdIterator = 0;
const roomLocks = new Set();

app.post('/create-room', jsonParser, async (req, res) => {
    const availableServer = await gameServerRepository.search().where('available').true().return.first();
    availableServer.available = false;
    await gameServerRepository.save(availableServer);
    await adjustServers();

    const passcode = makeid(5);
    const port = availableServer.port;
    await roomRepository.createAndSave({
        id: ++roomIdIterator,
        port,
        message: req.body.message,
        passcode,
        password: req.body.password,
        hasPassword: req.body.hasPassword,
        numPlayers: 0,
        started: false,
    });

    let resBody = {
        port,
        passcode,
    };
    res.end(JSON.stringify(resBody));
});

app.post('/join-room', jsonParser, async (req, res) => {
    const room = await roomRepository.search().where('id').equals(req.body.roomId).return.first();
    let forbiddenMessage = null;
    if (room === null) {
        forbiddenMessage = 'The room does not exist';
    } else if (room.hasPassword && req.body.password !== room.password) {
        forbiddenMessage = 'Password incorrect';
    } else if (room.started) {
        forbiddenMessage = 'The room has started game';
    } else if (room.numPlayers >= 5 - req.body.numPlayers) {
        forbiddenMessage = 'The room is full';
    } 
    if (forbiddenMessage !== null) {
        res.status(403);
        res.end(forbiddenMessage);
        return;
    }
    let resBody = {
        port: room.port,
        passcode: room.passcode,
    };
    res.end(JSON.stringify(resBody));
});

app.post('/get-rooms', jsonParser, async (req, res) => {
    const result = await roomRepository.search().return.all();
    const rooms = result.map((room) => {
        return {
            roomId: room.id,
            message: room.message,
            numPlayers: room.numPlayers,
            started: room.started,
            hasPassword: room.hasPassword,
        };
    });
    res.end(JSON.stringify(rooms));
});

// game server calls

app.post('/player-join', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.numPlayers++;
        await roomRepository.save(room);
    });
    res.end();
});

app.post('/player-leave', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.numPlayers--;
        await roomRepository.save(room);
    });
    res.end();
});

app.post('/confirm-room', jsonParser, async (req, res) => {
    const port = req.body.port;
    const room = await roomRepository.search().where('port').equals(port).return.first();
    if (room == null || room.passcode !== req.body.passcode) {
        res.status(403);
    } else {
        res.status(200);
    }
    res.end();
});

app.post('/close-room', jsonParser, async (req, res) => {
    const port = req.body.port;

    await changeRoom(port, async (room) => {
        await roomRepository.remove(room.entityId);
    });

    const gameServer = await gameServerRepository.search().where('port').equals(port).return.first();
    gameServer.available = true;
    await gameServerRepository.save(gameServer);
    await adjustServers();

    res.end();
});

app.post('/start-game', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.started = true;
        await roomRepository.save(room);
    });
    res.end();
});

app.post('/end-game', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.started = false;
        await roomRepository.save(room);
    });
    res.end();
});

// test endpoints

app.post('/rooms', jsonParser, async (req, res) => {
    const result = await roomRepository.search().return.all();
    res.end(JSON.stringify(result));
});

app.post('/game-servers', jsonParser, async (req, res) => {
    const gameServers = await gameServerRepository.search().return.all();
    res.end(JSON.stringify(gameServers));
});

// util functions

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function changeRoom(port, operation) {
    while (roomLocks.has(port)) {
        await sleep(10);
    }
    roomLocks.add(port);
    const room = await roomRepository.search().where('port').equals(port).return.first();
    await operation(room);
    roomLocks.delete(port);
}

async function adjustServers() {
    const availableServers = await gameServerRepository.search().where('available').true().return.all();
    if (availableServers.length < 1) {
        await launchServer();
    }
    if (availableServers.length > 1) {
        await closeServer(availableServers[0]);
    }
}

async function launchServer() {
    const gameServers = await gameServerRepository.search().return.all();
    const usedPorts = new Set(gameServers.map((g) => { return g.port}));
    let port = 7777;
    while (usedPorts.has(port)) ++port;

    const gameServer = gameServerRepository.createEntity()
    gameServer.port = port;
    gameServer.available = true;
    await gameServerRepository.save(gameServer);

    const command = `docker run -d --name gameServer${port} -p ${port}:7777/udp -v "${process.env.GAME}":/home -e PORT=${port} --add-host=host.docker.internal:host-gateway game`;
    childProcess.exec(command);
}

async function closeServer(gameServer) {
    await gameServerRepository.remove(gameServer.entityId);

    const hash = childProcess.execSync(`docker ps --filter name=gameServer${gameServer.port} -q`);
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
adjustServers();
console.log(`Running on http://${HOST}:${PORT}`);