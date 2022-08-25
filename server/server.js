import express from 'express';
import childProcess from 'child_process';
import bodyParser from 'body-parser';
import gameServerRepository from './entities/game-server.js';
import roomRepository from './entities/room.js';
import path from 'path';

adjustServers();

const jsonParser = bodyParser.json();

// public server calls

const publicPort = 80;
const publicHost = '0.0.0.0';
const publicServer = express();

let roomIdIterator = 0;
const roomLocks = new Set();

publicServer.post('/create-room', jsonParser, async (req, res) => {
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

publicServer.post('/join-room', jsonParser, async (req, res) => {
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

publicServer.post('/get-rooms', jsonParser, async (req, res) => {
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
    res.end(JSON.stringify({ rooms }));
});

publicServer.get('/download', jsonParser, async (req, res) => {
    const file = process.cwd() + '/Boom_setup.exe';
    res.download(file);
});

publicServer.get('/', jsonParser, async (req, res) => {
    const file = process.cwd() + '/WebGLBuild/index.html';
    res.sendFile(file);
});

publicServer.use(express.static('WebGLBuild'));

publicServer.listen(publicPort, publicHost);
console.log(`Public server running on http://${publicHost}:${publicPort}`);

// private server calls

const privatePort = 8080;
const privateHost = '0.0.0.0';
const privateServer = express();

privateServer.post('/player-join', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.numPlayers++;
        await roomRepository.save(room);
    });
    res.end();
});

privateServer.post('/player-leave', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.numPlayers--;
        await roomRepository.save(room);
    });
    res.end();
});

privateServer.post('/confirm-room', jsonParser, async (req, res) => {
    const port = req.body.port;
    const room = await roomRepository.search().where('port').equals(port).return.first();
    if (room == null || room.passcode !== req.body.passcode) {
        console.log("confirm denied");
        res.status(403);
    } else {
        console.log("confirm accepted");
        res.status(200);
    }
    res.end();
});

privateServer.post('/close-room', jsonParser, async (req, res) => {
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

privateServer.post('/start-game', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.started = true;
        await roomRepository.save(room);
    });
    res.end();
});

privateServer.post('/end-game', jsonParser, async (req, res) => {
    await changeRoom(req.body.port, async (room) => {
        room.started = false;
        await roomRepository.save(room);
    });
    res.end();
});

privateServer.listen(privatePort, privateHost);
console.log(`Private server running on http://${privateHost}:${privatePort}`);

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

    const command = `docker run -d --name gameServer${port} -p ${port}:7777 -v "${process.env.GAME}":/home -e PORT=${port} --add-host=host.docker.internal:host-gateway game`;
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
      result += characters.charAt(Math.floor(Math.random() * charactersLength));
   }
   return result;
}