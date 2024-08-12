-- Active: 1715864894173@@127.0.0.1@5432@admin@public
CREATE TABLE "Users"(  
    "Username" VARCHAR(50) NOT NULL PRIMARY KEY,
    "Password" VARCHAR(50) NOT NULL
);
CREATE TABLE "Chats"(  
    "Id" int NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    "Master" VARCHAR(50) NOT NULL, 
    "Source" VARCHAR(50) NOT NULL, 
    "GPT" VARCHAR(50) NOT NULL, 
    FOREIGN KEY ("Master") REFERENCES "Users" ("Username")
);
CREATE TABLE "Messages"(  
    "Id" int NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    "Time" TIMESTAMP NOT NULL,
    "Text" TEXT NOT NULL,
    "By" BOOL NOT NULL,
    "ChatId" INT NOT NULL,
    FOREIGN KEY ("ChatId") REFERENCES "Chats" ("Id")
);
CREATE TABLE "HabrNews"(  
    "Link" TEXT PRIMARY KEY,
    "Id" TEXT NOT NULL,
    "Time" TIMESTAMP NOT NULL,
    "TimeToRead" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Content" TEXT NOT NULL
);
CREATE TABLE "HabrTags"(  
    "Id" int NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    "Link" TEXT REFERENCES "HabrNews"("Link") NOT NULL,
    "Name" TEXT NOT NULL
);