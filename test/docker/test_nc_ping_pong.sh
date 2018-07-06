#!/bin/sh

/usr/share/azbridge/azbridge -x $RELAY_CXNSTRING -L 127.0.8.1:8888:a1 &
LOCAL_LISTENER_PID=$!
/usr/share/azbridge/azbridge -x $RELAY_CXNSTRING -R a1:9999 &
REMOTE_LISTENER_PID=$!
sleep 5 
#expected request: ping
echo "pong" | nc -l 9999 | xargs echo request: >> ~/testoutput.log 2>&1 &
sleep 1
#expected reply: pong
echo "ping" | nc 127.0.8.1 8888 | xargs echo reply: >> ~/testoutput.log 2>&1
sleep 5 
kill -INT $LOCAL_LISTENER_PID
kill -INT $REMOTE_LISTENER_PID
cat ~/testoutput.log