# Infrastructure README
This is the area where i'm putting the infrastructure to manage the statefulness and inmemory eventstore.

If you wish this aspect can be glossed over as it does not pertain directly to the test in and of itself.

However if you wish to read it, that's fine too. 

Please note - with respect to the main project the project is arranged into aggregateroots and 'features' 
 
While on a code basis, it's arranged by AggregateRoots, Entities ( in the original sense ) and ValueObjects.

The reason for this is that it is easier to determine intent and discriminate how the system works without having to follow the paths.

This is infrastructure is a scaleable CQRS infrastructure. 
 