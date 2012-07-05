Redis on Windows prototype
===
## What's new in this release

- Based on Redis 2.4.11
- Removed dependency on the pthreads library
- Improved the snapshotting (save on disk) algorithm. Implemented Copy-On-Write at the application level so snapshotting behavior is similar to the Linux version.
- added a Windows service to start and monitor one of more Redis instances

===
Special thanks to Dušan Majkic (https://github.com/dmajkic, https://github.com/dmajkic/redis/) for his project on GitHub that gave us the opportunity to quickly learn some on the intricacies of Redis code. His project also helped us to build our prototype quickly.

## Repo branches
- 2.4: save in foreground
- bksave: background save where we write the data to buffers first, then save to disk on a background thread. It is much faster than saving directly to disk, but it uses more memory. 
- bksavecow: Copy On Write at the application level, now the default branch.

## How to build Redis using Visual Studio

You can use the free Express Edition available at http://www.microsoft.com/visualstudio/en-us/products/2010-editions/visual-cpp-express.

Now *bksavecow* is the default branch.

- Open the solution file msvs\redisserver.sln in Visual Studio 10, and build.

    This should create the following executables in the msvs\$(Configuration) folder:

    - redis-server.exe
    - redis-benchmark.exe
    - redis-cli.exe
    - redis-check-dump.exe
    - redis-check-aof.exe

For your convinience all binaries and the MSI for the Redis-Watcher service will be available in the msvs/bin/release|debug directories.

### RedisWatcher
With this release we added a Windows Service that can be used to start and monitor one or more Redis instances, the service 
monitors the processes and restart them if they stop. 

You can find the project to build the service under the msvs\RedisWatcher directory. In the readme on the same location
you will find the instructions on how to build and use the service.

### Release Notes

This is a pre-release version of the software and is not yet fully tested. This is intended to be a 32bit release only. 
No work has been done in order to produce a 64bit version of Redis on Windows.
To run the test suite requires some manual work:

- The tests assume that the binaries are in the src folder, so you need to copy the binaries from the msvs folder to src. 
- The tests make use of TCL. This must be installed separately.
- To run the tests you need to have a Unix shell on your machine. To execute the tests, run the following command: `tclsh8.5.exe tests/test_helper.tcl`. 
  
If a Unix shell is not installed you may see the following error message: "couldn't execute "cat": no such file or directory".

### Plan for the next release

- Improve test coverage
- Fix some performance issues on the Copy On Write code
- Add 64bit support


 