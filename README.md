# WebAPI For Sql to Contact Object data sync : 
Created API project for inserting record using EF core in SQL and checking the Salesforce Object and by using REST API.
Below API fetches contact object fields:
URL : $"https://orgfarm-93b146fa03-dev-ed.develop.my.salesforce.com/services/data/v60.0/sobjects/Contact/describe"
We need access token to access the above URL by calling "https://login.salesforce.com/services/oauth2/authorize".
Postman provides the set of URLs to work with salesforce and get the accessToken

After getting all the fields from API response with the help of DeserializeObject, converting object into stongly typed class.
If any mismatch at field size of field is missing it will not allow the request to sync the SQL to SalesForce object.

# API Project Structure : 
Making use of Interface to seprate the repository and services and to achieve the abstraction.
Keeping my DB calls in Repository forlder. And Business logic to service folder.
Inside the model folder creating the creating neccessary Request & Response classes.
Making use of DTO to make sure data does not exposed to outside world.
Registering Repository & Services in Program.cs file.
With the help of DI container injecting services and accessing with constructor.

# Future Enhancement : 
Impliment the clean architeture style to seprate the folder into class library.
we can impliment the Authentication mechanism as well to allow only authenticated user.
Generation of access token from auth service instead of calling the post man API.
Instead of inserting one by one record we can use bulk upload mechanism by passing data in excel.
