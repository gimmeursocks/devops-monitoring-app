const express = require('express');
const http = require('http');
const { Server } = require("socket.io");
const { Pool } = require('pg');
const path = require('path');

const app = express();
const server = http.createServer(app);
const io = new Server(server);

const PORT = process.env.PORT || 80;

const pool = new Pool({
  user: process.env.POSTGRES_USER || 'postgres',
  host: process.env.POSTGRES_HOST || 'db',
  database: 'postgres',
  password: process.env.POSTGRES_PASSWORD || 'postgres',
  port: process.env.POSTGRES_PORT || 5432,
});

app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));

app.get('/', (req, res) => {
  res.render('index');
});

const getVotes = async () => {
    try {
        const result = await pool.query('SELECT vote, COUNT(id) AS count FROM votes GROUP BY vote');
        const votes = { a: 0, b: 0 };
        result.rows.forEach(row => {
            if (row.vote === 'a') votes.a = parseInt(row.count);
            if (row.vote === 'b') votes.b = parseInt(row.count);
        });
        return votes;
    } catch (err) {
        console.error('Error querying votes:', err);
        return { a: 0, b: 0 };
    }
};

io.on('connection', (socket) => {
  console.log('A user connected');
  socket.on('disconnect', () => {
    console.log('User disconnected');
  });
});

setInterval(async () => {
    const votes = await getVotes();
    io.emit('votes', votes);
}, 1000);


server.listen(PORT, '0.0.0.0', () => {
  console.log(`Result app listening on port ${PORT}`);
});
