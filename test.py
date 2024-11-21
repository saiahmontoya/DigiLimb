from pymongo import MongoClient

# Test file to show user name and password for all objects in data
client = MongoClient('mongodb+srv://ryangallagher01:FtTgpOQcUsDo01o7@cluster0.7mcrx.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0')
db = client['DigiLimb']

pipeline = [
    {"$project": { "username": 1,
                   "password": 1}}
]
result = db.data.aggregate(pipeline)
for doc in result:
    print(doc)