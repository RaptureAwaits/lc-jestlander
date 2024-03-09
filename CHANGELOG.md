# 1.2.0
Added different behaviour for host and non-host clients in light of the fact that only the host's popup timer is accurate. Host players will hear a clean(ish) transition between audio clips, whereas non-host players will have to put up with the "extended" windup.

# 1.1.0
Extended (poorly) the windup clip so audio doesn't jarringly loop in the event of a desync. Added a pop sound effect. Added some primitive logging to help me figure out why the timing is janky sometimes.

# 1.0.0
Initial release.