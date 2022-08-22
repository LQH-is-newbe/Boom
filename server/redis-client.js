import { Client } from 'redis-om';

const client = new Client()
await client.open('redis://localhost:6379')

const aString = await client.execute(['PING']);
console.log(aString);

export default client;