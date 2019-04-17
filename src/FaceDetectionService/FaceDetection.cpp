// FaceDetection.cpp : Ce fichier contient la fonction 'main'. L'exécution du programme commence et se termine à cet endroit.
//

#include <iostream>
#include <vector>
#include <filesystem>

#include <opencv2/opencv.hpp>
#include <opencv2/highgui/highgui.hpp>

#include "facedetectcnn.h"

#include <amqp.h>
#include <amqp_tcp_socket.h>
#include "utils.h"


#define DETECT_BUFFER_SIZE 0x20000
#define SUMMARY_EVERY_US 1000000



int oldMain(int, char*);
int detectFaces(const char *, const char *);


namespace fs = std::filesystem;




static void run(amqp_connection_state_t conn) {
	uint64_t start_time = now_microseconds();
	int received = 0;
	int previous_received = 0;
	uint64_t previous_report_time = start_time;
	uint64_t next_summary_time = start_time + SUMMARY_EVERY_US;

	amqp_frame_t frame;

	uint64_t now;

	for (;;) {
		amqp_rpc_reply_t ret;
		amqp_envelope_t envelope;

		now = now_microseconds();
		if (now > next_summary_time) {
			int countOverInterval = received - previous_received;
			double intervalRate =
				countOverInterval / ((now - previous_report_time) / 1000000.0);
			printf("%d ms: Received %d - %d since last report (%d Hz)\n",
				(int)(now - start_time) / 1000, received, countOverInterval,
				(int)intervalRate);

			previous_received = received;
			previous_report_time = now;
			next_summary_time += SUMMARY_EVERY_US;
		}

		amqp_maybe_release_buffers(conn);
		ret = amqp_consume_message(conn, &envelope, NULL, 0);

		if (AMQP_RESPONSE_NORMAL != ret.reply_type) {
			if (AMQP_RESPONSE_LIBRARY_EXCEPTION == ret.reply_type &&
				AMQP_STATUS_UNEXPECTED_STATE == ret.library_error) {
				if (AMQP_STATUS_OK != amqp_simple_wait_frame(conn, &frame)) {
					return;
				}

				if (AMQP_FRAME_METHOD == frame.frame_type) {
					switch (frame.payload.method.id) {
					case AMQP_BASIC_ACK_METHOD:
						/* if we've turned publisher confirms on, and we've published a
						 * message here is a message being confirmed.
						 */
						break;
					case AMQP_BASIC_RETURN_METHOD:
						/* if a published message couldn't be routed and the mandatory
						 * flag was set this is what would be returned. The message then
						 * needs to be read.
						 */
					{
						amqp_message_t message;
						ret = amqp_read_message(conn, frame.channel, &message, 0);
						if (AMQP_RESPONSE_NORMAL != ret.reply_type) {
							return;
						}

						amqp_destroy_message(&message);
					}

					break;

					case AMQP_CHANNEL_CLOSE_METHOD:
						/* a channel.close method happens when a channel exception occurs,
						 * this can happen by publishing to an exchange that doesn't exist
						 * for example.
						 *
						 * In this case you would need to open another channel redeclare
						 * any queues that were declared auto-delete, and restart any
						 * consumers that were attached to the previous channel.
						 */
						return;

					case AMQP_CONNECTION_CLOSE_METHOD:
						/* a connection.close method happens when a connection exception
						 * occurs, this can happen by trying to use a channel that isn't
						 * open for example.
						 *
						 * In this case the whole connection must be restarted.
						 */
						return;

					default:
						fprintf(stderr, "An unexpected method was received %u\n",
							frame.payload.method.id);
						return;
					}
				}
			}

		}
		else {
			printf("Delivery %u, exchange %.*s routingkey %.*s\n",
				(unsigned)envelope.delivery_tag, (int)envelope.exchange.len,
				(char *)envelope.exchange.bytes, (int)envelope.routing_key.len,
				(char *)envelope.routing_key.bytes);

			amqp_dump(envelope.message.body.bytes, envelope.message.body.len);

			char *str = new char[envelope.message.body.len+1];
			memcpy_s(str, envelope.message.body.len,
				envelope.message.body.bytes,
				envelope.message.body.len);
			memset(str + envelope.message.body.len, 0, 1);
			fs::path path = str;
			std::string fileName = path.stem().string() + "_Mod" + path.extension().string();
			const fs::path imagePath = path;
			bool fileExists = fs::exists(path);
			fs::path output = path.remove_filename();
			output += fs::path(fileName);
			std::cout << "Path exists: " << fileExists
				<< std::endl << "Path Directory: " << path <<std::endl
				<<"Predicted output: "<< output << std::endl;
			if (fileExists) {
				detectFaces(imagePath.string().c_str(),output.string().c_str());
			}

			//amqp_basic_ack

			delete[] str;
			amqp_destroy_envelope(&envelope);
		}

		received++;
	}
}

using namespace cv;

#define EXCHANGE_NAME "Image_Exchange"
#define EXCHANGE_TYPE "topic"
#define IMAGE_QUEUE_NAME "Images_Queue"
#define ALL_QUEUE_NAME "AllTopic_Queue"

#define BINDING_KEY "image.path"

#define HOSTNAME "localhost"
#define LPORT 5672

int main(int argc ,char *argv[])
{
	/*oldMain(argc, argv);*/

	char const *hostname;
	int port, status;
	char const *exchange;
	char const *bindingkey;
	char const *exchangetype;
	amqp_socket_t *socket = NULL;
	amqp_connection_state_t conn;

	amqp_bytes_t queuename;

	hostname = HOSTNAME;
	port = LPORT;
	exchange = EXCHANGE_NAME;
	bindingkey = BINDING_KEY;
	exchangetype = EXCHANGE_TYPE;

	conn = amqp_new_connection();

	socket = amqp_tcp_socket_new(conn);
	if (!socket) {
		die("creating TCP socket");
	}

	status = amqp_socket_open(socket, hostname, port);
	if (status) {
		die("opening TCP socket");
	}

	die_on_amqp_error(amqp_login(conn, "/", 0, 131072, 0, AMQP_SASL_METHOD_PLAIN,
		"guest", "guest"),
		"Logging in");
	amqp_channel_open(conn, 1);
	die_on_amqp_error(amqp_get_rpc_reply(conn), "Opening channel");
	amqp_exchange_declare(conn, 1, amqp_cstring_bytes(exchange),
		amqp_cstring_bytes(exchangetype), 0, 0, 0, 0,
		amqp_empty_table);
	die_on_amqp_error(amqp_get_rpc_reply(conn), "Declaring exchange");

	{
		amqp_queue_declare_ok_t *r = amqp_queue_declare(
			conn, 1, amqp_cstring_bytes(IMAGE_QUEUE_NAME), 0, 1, 0, 0, amqp_empty_table);
		die_on_amqp_error(amqp_get_rpc_reply(conn), "Declaring queue");
		queuename = amqp_bytes_malloc_dup(r->queue);
		if (queuename.bytes == NULL) {
			fprintf(stderr, "Out of memory while copying queue name");
			return 1;
		}
	}

	amqp_queue_bind(conn, 1, queuename, amqp_cstring_bytes(exchange),
		amqp_cstring_bytes(bindingkey), amqp_empty_table);

	die_on_amqp_error(amqp_get_rpc_reply(conn), "Binding queue");

	amqp_basic_consume(conn, 1, queuename, amqp_empty_bytes, 0, 1, 0,
		amqp_empty_table);
	die_on_amqp_error(amqp_get_rpc_reply(conn), "Consuming");

	run(conn);

	die_on_amqp_error(amqp_channel_close(conn, 1, AMQP_REPLY_SUCCESS),
		"Closing channel");
	die_on_amqp_error(amqp_connection_close(conn, AMQP_REPLY_SUCCESS),
		"Closing connection");
	die_on_error(amqp_destroy_connection(conn), "Ending connection");


}

int detectFaces(const char * path, const char * output) {

	Mat image = imread(path);
	if (image.empty())
	{
		fprintf(stderr, "Can not load the image file %s.\n", path);
		return -1;
	}


	int * pResults = NULL;
	//pBuffer is used in the detection functions.
	//If you call functions in multiple threads, please create one buffer for each thread!
	unsigned char * pBuffer = (unsigned char *)malloc(DETECT_BUFFER_SIZE);
	if (!pBuffer)
	{
		fprintf(stderr, "Can not alloc buffer.\n");
		return -1;
	}


	///////////////////////////////////////////
	// CNN face detection 
	// Best detection rate
	//////////////////////////////////////////
	//!!! The input image must be a RGB one (three-channel)
	//!!! DO NOT RELEASE pResults !!!
	pResults = facedetect_cnn(pBuffer, (unsigned char*)(image.ptr(0)), image.cols, image.rows, (int)image.step);

	printf("%d faces detected.\n", (pResults ? *pResults : 0));
	Mat result_cnn = image.clone();
	//print the detection results
	for (int i = 0; i < (pResults ? *pResults : 0); i++)
	{
		short * p = ((short*)(pResults + 1)) + 142 * i;
		int x = p[0];
		int y = p[1];
		int w = p[2];
		int h = p[3];
		int confidence = p[4];
		int angle = p[5];

		printf("face_rect=[%d, %d, %d, %d], confidence=%d, angle=%d\n", x, y, w, h, confidence, angle);
		if (confidence > 85) {
			rectangle(result_cnn, Rect(x, y, w, h), Scalar(0, 255, 0), 2);
		}
	}
	//imshow("result_cnn", result_cnn);

	ofstream myfile;
	if (output) {
		imwrite(output, result_cnn);
	}


	//waitKey();

	//release the buffer
	free(pBuffer);

}




int oldMain(int argc, char* argv[]) {
	if (argc != 2)
	{
		printf("Usage: %s <image_file_name>\n", argv[0]);
		return -1;
	}

	detectFaces(argv[1],nullptr);
	
	//imwrite("result.jpg", result_cnn);

	return 0;
}

