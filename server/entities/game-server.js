import { Entity, Schema } from 'redis-om';
import client from '../redis-client.js';

class GameServer extends Entity {};

const gameServerSchema = new Schema(GameServer, {
    port: { type: 'number' },
    available: { type: 'boolean' },
}, {
    dataStructure: 'HASH',
});

const gameServerRepository = client.fetchRepository(gameServerSchema)
await gameServerRepository.createIndex();

export default gameServerRepository;