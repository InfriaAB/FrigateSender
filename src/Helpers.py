import traceback, sys, asyncio, threading

# used to help format stacktrace for logging
def format_stacktrace():
    parts = ["Traceback (most recent call last):\n"]
    parts.extend(traceback.format_stack(limit=25)[:-2])
    parts.extend(traceback.format_exception(*sys.exc_info())[1:])
    return "".join(parts)


_loop = asyncio.new_event_loop()
_thr = threading.Thread(target=_loop.run_forever, name="Async Runner", daemon=True)
# Run async methods in sync.
def run_async(coro): 
    if not _thr.is_alive():
        _thr.start()
    future = asyncio.run_coroutine_threadsafe(coro, _loop)
    return future.result()