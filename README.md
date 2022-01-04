[![Tests](https://github.com/Kezzo/reliable-udp/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/Kezzo/reliable-udp/actions/workflows/tests.yml)

# Reliable UDP

.NET library that implements reliable (and optionally ordered) UDP delivery. 
The design is inspired by Gaffer on Games article: https://gafferongames.com/post/reliable_ordered_messages/.
Since the library targets the [.NET Standard 1.0](https://dotnet.microsoft.com/en-us/platform/dotnet-standard) any .NET project using any framework is supported including Unity 2018.1 and up.

The library guarantees that messages are:
- sent reliably (individual message can be optionally sent unreliably)
- received without duplicates
- received in order (optional)

> :exclamation: When using this library it is vital that the client and server both use this library to send and receive messages.

The library does **not** exercise head of line blocking in the traditional sense, but rather re-sends messages until a confirmation (ack) of their receival has been received. Messages are sent again after the previous sent time + RTT has passed. This could in theory lead to duplicate messages, but the library sorts those cases out. More recent messages are still continously sent and when received they're buffered and made available once the previous lost message has arrived.
> :warning: When the library is used to received messages *in order* a delay occurs for all messages that arrive after a lost message should've arrived until the lost message was sent again and received successfully. The delay is usually equal to the length of the RTT. If this behavior is not acceptable, the library can provide messages immediately when they have been received (while keeping them reliable) which can lead to them not being in the same order as they have been sent in.

In addition the library provides an interface to **serialize/deserialize** messages and handles packing messages together into packets. A packet containing multiple messages is kept under **508 bytes** (can be configured differently). This is due the [MUT](https://en.wikipedia.org/wiki/Maximum_transmission_unit) of the ip protocol to decrease the occurances of packet fragmentations and therefore decrease the chance of losing a packet (a missing fragment of a ip packet will cause the entire ip packet to be dropped). [508 bytes](https://serverfault.com/questions/246508/how-is-the-mtu-is-65535-in-udp-but-ethernet-does-not-allow-frame-size-more-than) is chosen because it leaves enough room for a potentially expanded ip header to support IPv6 and routers adding aditional data to the header.
**Bigger messages** than 508 bytes can be transmitted too, but will be send in a separate packet without other messages packed into the same packet. No additional action for this has to be done by the user of this library to make this work. Bigger messages have the same reliability, in-order and duplicate guarantees as smaller packets, but could result in higher packet loss ratios.

The library strives for high quality by:
- keeping the library code clean and simple
- no external third-party dependencies (see [.csproj file](src/ReliableUdp.csproj))
- testing broadly with unit- and integration-tests
- limiting feature-set to bare needed minimum
- implementing a clear and easy to use API
- providing good documentation and examples

Roadmap:
- [x] Reliable ordered delivery
- [ ] Add examples
- [ ] Support for unreliable messages
- [ ] Support for unordered message receival
- [ ] Retransmission based on RTT
- [ ] Optimize memory allocation
- [ ] DTLS support