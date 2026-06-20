# CannedNet
A RecNet API Reimplementation written in C#

Currently supported version: 2021-01-29 (There is a 2023 branch currently being worked on, and is refactoring a lot of things internally. If you are wanting to contribute to 2023, please use the `2023-Refactor` branch.)

Contact: 

[CannedNet Discord](https://discord.gg/MhSZ3UFTKG)

`@lapisosc` on Twitter

`lapiscreates` on Libera IRC

TODO:

- Player PFP changing
- Outfit saving
- CDN server
- Play tab
- Clubs Server
- People
- Friend requests
- Invites

## Known Issues
- Play tab will randomly try to use the new UI split-test randomly, I want to try and prevent this as the endpoints are a bit weird, and I'd rather not have the inconsisency between sessions.
- All presences will say [VERSION MISMATCH]
- Even if a player logs out, it will permenatly say they are online.
- Image server returns an image, but improperly as the game doesn't handle the image and display it.
- You are only able to go to private instances of rooms

## Why?

Rec Room has now publicly announced that they are shutting down forever on **June 1st of this year.**

This game has been essential to me, allowing me to find pretty much everyone I know today and it really sucks to see it go away.

So I want *anyone* to be able to host their own Rec Room API, and just be able to connect to it, and run it with ease, so I'm starting this project.

## Pull Requests

Pull Requests are **VERY** welcome in this project, but please, ***PLEASE*** do not just ask some random AI chatbot to make features and then don't test it in the slightest.

Use little to none AI in this project, this project should be stable.

If AI is overused or completely has done your PR, even if it works perfectly, it will be declined.

Try to minimize merge conflicts too, although it may be impossible to avoid.
