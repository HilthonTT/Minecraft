# Security policy

## Supported versions

This is a hobby project with a single line of development. Fixes go on `main` and into the next release; older
releases are not patched.

## Reporting a vulnerability

Please report privately rather than opening a public issue, through
[GitHub's private advisory form](https://github.com/HilthonTT/Minecraft/security/advisories/new). If that is
not an option, email hans.tandt@gmail.com.

Include what the problem is, how to reproduce it, and what an attacker gets out of it. Expect a first reply
within a week or so — this is worked on in spare time.

## What is in scope

The multiplayer path is the part worth looking at. The server accepts connections and reads packets written by
whoever is on the other end, so anything reachable from there is interesting:

- Packet handling in `Minecraft.Core/Network/` and the binary reader in `Minecraft.Core/IO/`
- Malformed or hostile packet data crashing or hanging a server, or reading memory it should not
- A client causing world changes it has no business making
- Path traversal or arbitrary writes through world names and the save format in `Minecraft.Core/Worlds/Storage/`

There is no authentication, no encryption and no sandboxing between players. Run a server for people you
trust, and do not expose one to the open internet expecting it to hold up. Reports that amount to "an untrusted
player can grief the world" describe a known limitation rather than a vulnerability.

Bugs in OpenTK or the .NET runtime belong upstream, though a note here is welcome if this project is affected.
