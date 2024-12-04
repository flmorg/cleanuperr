## LOGGING__ENHANCED

Some logs may contain information that is hard to read. Enhancing these logs usually comes with the cost of additional calls to the APIs.

If enabled, logs like this

```movie search triggered | http://localhost:7878/ | movie ids: 1, 2```

will transform into

```movie search triggered | http://localhost:7878/ | [Speak No Evil][The Wild Robot]```