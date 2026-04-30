# StreetZ - Networked Multiplayer Basketball

A Networked Multiplayer Basketball Game: Server-Authoritative State Synchronisation 
for Real-Time Play over the Public Internet.

**Author:** Michael Alexander Odibo (31801268)  
**Supervisor:** Dr Martin Lester  
**Module:** CS3IP - Individual Project, University of Reading  
**Submission:** 1 May 2026

## Project structure

- `Assets/Scripts/` - all C# scripts (21 files, ~4000 lines)
- See dissertation Appendix A for the per-file responsibility table

## Building

Built with Unity 6 (LTS). Open the project in Unity Hub, allow it to import 
packages (Netcode for GameObjects, Unity Lobby, Unity Relay, Unity Authentication, 
Cinemachine, TextMeshPro), then open the GameScene from `Assets/Scenes/`.

## Networking

Uses Unity Relay for transport, Unity Lobby for matchmaking, anonymous Unity 
Authentication for sign-in. No self-hosted infrastructure required - the host 
allocates a Relay slot at runtime and shares the resulting six-character join 
code with the joining client.
