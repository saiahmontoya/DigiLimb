from pymongo import MongoClient

# Connect to MongoDB
client = MongoClient('mongodb+srv://ryangallagher01:FtTgpOQcUsDo01o7@cluster0.7mcrx.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0')
db = client['DigiLimb']
collection = db['data']  # Replace with your collection name

# Function to retrieve all data
def retrieve_all_data():
    """Retrieve and display all documents."""
    pipeline = [{"$project": {"username": 1, "password": 1}}]
    result = collection.aggregate(pipeline)
    for doc in result:
        print(doc)

# Function to search for an entity
def search_entity():
    """Search for an entity by field and value."""
    field = input("Enter the field to search by (e.g., username): ")
    value = input(f"Enter the value to search for in '{field}': ")
    query = {field: value}
    result = collection.find(query, {"_id": 0, "username": 1, "password": 1})
    found = False
    for doc in result:
        print(doc)
        found = True
    if not found:
        print("No matching entity found.")

# Function to add an entity
def add_entity():
    """Add a new entity to the collection."""
    username = input("Enter username: ")
    password = input("Enter password: ")
    new_entity = {"username": username, "password": password}
    result = collection.insert_one(new_entity)
    print(f"Entity added with ID: {result.inserted_id}")

# Function to remove an entity
def remove_entity():
    """Remove an entity based on field and value."""
    field = input("Enter the field to match for deletion (e.g., username): ")
    value = input(f"Enter the value to match for deletion in '{field}': ")
    query = {field: value}
    result = collection.delete_one(query)
    if result.deleted_count > 0:
        print("Entity deleted successfully.")
    else:
        print("No matching entity found.")

# Main menu function
def main_menu():
    """Display the menu and handle user input."""
    while True:
        print("\nMenu:")
        print("1. Retrieve all data")
        print("2. Search for an entity")
        print("4. Add an entity")
        print("5. Remove an entity")
        print("0. Exit")
        choice = input("Enter your choice: ")

        if choice == '1':
            retrieve_all_data()
        elif choice == '2':
            search_entity()
        elif choice == '4':
            add_entity()
        elif choice == '5':
            remove_entity()
        elif choice == '0':
            print("Exiting the system. Goodbye!")
            break
        else:
            print("Invalid choice. Please try again.")

# Run the program
if __name__ == "__main__":
    main_menu()
