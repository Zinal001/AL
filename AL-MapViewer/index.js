'use strict';

const express = require("express");
const path = require("path");
const mapRoute = require("./routes/map");
const npcRoute = require("./routes/npc");
const db = require("./db");

const app = module.exports = express();

app.use(express.static(path.join(__dirname, "public")));
app.set("views", path.join(__dirname, "views"));
app.set("view engine", ".ejs");

app.get("/", mapRoute);
app.get("/map", mapRoute);
app.get("/map/:mapName", mapRoute);
app.get("/map/:mapName/:mapX", mapRoute);
app.get("/map/:mapName/:mapX/:mapY", mapRoute);
app.get("/map/:mapName/:mapX/:mapY/:serverId", mapRoute);

app.get("/npc/:id", npcRoute);
app.get("/npc/:id/:mapName", npcRoute);
app.get("/npc/:id/:mapName/:serverId", npcRoute);


db.init().then(() => {
    app.listen(3000, () => console.log("Listening on port 3000"));
}).catch((err) => {
    console.log("Error connecting to db: ", err);
    process.exit(1);
});

const onShutdown = () => {
    db.shutdown();
};

process.on("SIGINT", onShutdown);
process.on("SIGTERM", onShutdown);
process.on("SIGUSR2", onShutdown);