//db.auth("root", "root");

db.createUser(
	{
		user: "root",
		pwd: "root",
		roles: [
			{
				role: "readWrite",
				db: "almapper"
			}
		]
	}
);

db2 = db.getSiblingDB("almapper");

db.Servers.insertOne({
	"ServerKey": "Default",
	"Name": "Adventure Land",
	"Url": "https://adventure.land",
	"LastGenerated": null
});
db.Servers.insertOne({
	"ServerKey": "thmsn",
	"Name": "thmsn",
	"Url": "http://thmsn.adventureland.community",
	"LastGenerated": null
});