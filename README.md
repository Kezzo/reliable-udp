[![Tests](https://github.com/Kezzo/reliable-udp/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/Kezzo/reliable-udp/actions/workflows/tests.yml)

# Reliable UDP

.NET library that implements reliable (and optional ordered) UDP delivery. 
The design is based on Gaffer on Games article: https://gafferongames.com/post/reliable_ordered_messages/

The library guarantees that messages are:
- received in order
- sent reliably
- received without duplicates

The library does not exercise head of line blocking, but rather re-sends messages (based on RTT) until they're received. More recent messages are still continously sent and when received they're buffered and made available once the previous lost message has arrived.
> :warning: When messages are received in order a delay occurs for all messages that arrive after a lost message should've arrived until the lost message was sent again and received successfully. The delay is usually equal to the length of the RTT. If this behavior is not acceptable, messages need to be received out of order (while keeping them reliable).

The library strives for high quality by:
- keeping the library code clean and simple
- minimize external dependencies
- testing broadly with unit- and integration-tests
- limiting feature-set to bare needed minimum
- implementing a clear and easy to use API
- providing good documentation and examples

Roadmap:
- [ ] Reliable ordered delivery
- [ ] Support for unreliable messages
- [ ] Support for unordered message receival
- [ ] Add example project
- [ ] Optimize memory allocation
- [ ] DTLS support