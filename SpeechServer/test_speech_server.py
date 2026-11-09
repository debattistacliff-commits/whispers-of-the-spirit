import io
import json
import threading
import unittest
import urllib.error
import urllib.request
import wave
from http.server import ThreadingHTTPServer
from types import SimpleNamespace

import speech_server as app


def silence():
    stream = io.BytesIO()
    with wave.open(stream, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(48000)
        w.writeframes(b'\0' * 96000)
    return stream.getvalue()


class FakeModel:
    def transcribe(self, data, **kwargs):
        self.options = kwargs
        return iter([SimpleNamespace(start=0, end=1, text='test', no_speech_prob=0.1, avg_logprob=-0.2, compression_ratio=1.0),
                     SimpleNamespace(start=1, end=2, text='rejected noise', no_speech_prob=0.9, avg_logprob=-0.2, compression_ratio=1.0)]), None


class Tests(unittest.TestCase):
    def test_wave(self):
        app.validate_wave(silence())
        for bad in (b'no wave', silence()[:100]):
            with self.assertRaises(ValueError): app.validate_wave(bad)

    def test_languages_and_filtering(self):
        for language in ('mt', 'it', 'en'):
            model = FakeModel()
            result = app.transcribe_audio(model, silence(), language)
            self.assertIn('test', result['text'])
            self.assertNotIn('rejected', result['text'])
            self.assertEqual(model.options['language'], language)
            self.assertEqual(model.options['task'], 'transcribe')
            self.assertFalse(model.options['condition_on_previous_text'])
            self.assertTrue(model.options['vad_filter'])

    def test_http_auth_and_request(self):
        server = ThreadingHTTPServer(('127.0.0.1', 0), app.handler_factory(FakeModel(), 'test-token'))
        thread = threading.Thread(target=server.serve_forever, daemon=True); thread.start()
        url = f'http://127.0.0.1:{server.server_address[1]}'
        try:
            with self.assertRaises(urllib.error.HTTPError) as caught: urllib.request.urlopen(url + '/health')
            self.assertEqual(caught.exception.code, 401)
            req = urllib.request.Request(url + '/transcribe?language=it', data=silence(), headers={'Authorization': 'Bearer test-token', 'Content-Type': 'audio/wav'})
            with urllib.request.urlopen(req) as response: result = json.load(response)
            self.assertEqual(result['language'], 'it')
        finally:
            server.shutdown(); server.server_close(); thread.join()


if __name__ == '__main__': unittest.main()
