import { Entity, Schema } from 'redis-om';
import client from '../redis-client.js';

class Room extends Entity {};

const roomSchema = new Schema(Room, {
    id: { type: 'number' },
    port: { type: 'number' },
    started: { type: 'boolean' },
    message: { type: 'string' },
    passcode: { type: 'string' },
    password: { type: 'string' },
    hasPassword: { type: 'boolean' },
    numPlayers: { type: 'number' },
}, {
    dataStructure: 'HASH',
});

const roomRepository = client.fetchRepository(roomSchema);
await roomRepository.createIndex();

export default roomRepository;