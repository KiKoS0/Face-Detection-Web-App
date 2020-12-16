# Face Detection Web App
A .net core web app web app that uses a C++ face detection as a microservice with RabbitMQ as a Message Broker.


![Examples](/readme/diagram.png "Flow Diagram")


## Build
The easiest way to build the project is to: 
1. Download install OpenCV and declare an environment variable OPENCV_DIR with OpenCV path.
2. Download and install [RabbitMQ](https://www.rabbitmq.com/).
3. Download and install [MongoDB](https://www.mongodb.com/).

Note: The project is configured with the default rabbitmq-mongodb settings, so if you wish to change anything (ports for instance) you should modify the project settings accordingly.

### Dependencies
* [OpenCV](https://github.com/opencv/opencv)
* [libfacedetection](https://github.com/ShiqiYu/libfacedetection/)

## Example
![Examples](/readme/image.png "Detection example")



