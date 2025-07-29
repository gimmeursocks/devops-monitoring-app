import os
from flask import Flask, render_template_string, request, redirect, url_for
import redis
from redis.exceptions import ConnectionError as RedisConnectionError

app = Flask(__name__)

def get_redis_conn():
    redis_host = os.environ.get('REDIS_HOST', 'redis')
    redis_port = int(os.environ.get('REDIS_PORT', 6379))
    return redis.Redis(host=redis_host, port=redis_port, db=0, socket_connect_timeout=2)

@app.route("/", methods=['GET', 'POST'])
def index():
    voter_id = request.cookies.get('voter_id')
    if not voter_id:
        voter_id = os.urandom(16).hex()

    vote = None

    if request.method == 'POST':
        try:
            redis_conn = get_redis_conn()
            vote = request.form['vote']
            data = {'voter_id': voter_id, 'vote': vote}
            redis_conn.rpush('votes', str(data))
        except RedisConnectionError:
            return "Error: Could not connect to Redis.", 500
        except Exception as e:
            return f"An error occurred: {e}", 500

    template = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Cats vs Dogs Vote</title>
        <style>
            body { font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #f4f4f9; color: #333; text-align: center; margin: 0; padding: 40px; }
            .container { max-width: 600px; margin: auto; background: #fff; padding: 20px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.1); }
            h1 { color: #4a4a4a; }
            p { font-size: 1.1em; }
            .options { display: flex; justify-content: center; gap: 20px; margin: 30px 0; }
            .vote-button {
                background-color: #5c67f2;
                color: white;
                border: none;
                padding: 15px 30px;
                font-size: 1.2em;
                font-weight: bold;
                border-radius: 50px;
                cursor: pointer;
                transition: background-color 0.3s, transform 0.2s;
                box-shadow: 0 2px 4px rgba(0,0,0,0.2);
            }
            .vote-button:hover { background-color: #4a54e1; transform: translateY(-2px); }
            .vote-button.dogs { background-color: #f28e5c; }
            .vote-button.dogs:hover { background-color: #e17a4a; }
            #voted-message { display: {% if vote %}block{% else %}none{% endif %}; margin-top: 20px; font-size: 1.2em; color: #28a745; font-weight: bold; }
        </style>
    </head>
    <body>
        <div class="container">
            <h1>Cats vs. Dogs!</h1>
            <p>Which one is the better pet? Cast your vote now!</p>
            <div class="options">
                <form method="POST" action="/">
                    <button name="vote" value="a" class="vote-button cats">Cats</button>
                </form>
                <form method="POST" action="/">
                    <button name="vote" value="b" class="vote-button dogs">Dogs</button>
                </form>
            </div>
            <div id="voted-message">
                Thanks for your vote for {{ 'Cats' if vote == 'a' else 'Dogs' }}!
            </div>
        </div>
        <script>
            document.cookie = `voter_id={{ voter_id }}`;
        </script>
    </body>
    </html>
    """
    resp = app.make_response(render_template_string(template, vote=vote, voter_id=voter_id))
    resp.set_cookie('voter_id', voter_id)
    return resp

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=80)
