import numpy as np
import numexpr as ne
from flask import Flask, render_template, json, request
from flask_cors import CORS

import json 

class NumpyEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.ndarray):
            return obj.tolist()
        return json.JSONEncoder.default(self, obj)

app = Flask(__name__)
CORS(app);

@app.route('/init_data', methods=['GET'])
def init_data():
    x_init = np.arange(-10, 10, 0.7);
    y_init = np.arange(-10, 10, 0.7);
    
    xgrid, ygrid = np.meshgrid (x_init, y_init)
    zgrid = np.sin((xgrid**2 + ygrid**2)**(1/2)) / 1.3

    return  json.dumps(zgrid, cls=NumpyEncoder)

@app.route('/get_data', methods=['GET', 'POST'])
def get_data():
    name = request.form['name'];
    xstart = request.form['xstart'];
    xend = request.form['xend'];
    ystart = request.form['ystart'];
    yend = request.form['yend'];
    step = request.form['step'];

    xRange = np.arange(float(xstart), float(xend), float(step));
    yRange = np.arange(float(ystart), float(yend), float(step));
    
    x, y = np.meshgrid(xRange, yRange)

    zgrid = ne.evaluate(name)

    return  json.dumps(zgrid, cls=NumpyEncoder)
 


@app.after_request
def add_headers(response):
    response.headers.add('Content-Type', 'application/json')
    response.headers.add('Access-Control-Allow-Origin', '*')
    response.headers.add('Access-Control-Allow-Methods', 'PUT, GET, POST, DELETE, OPTIONS')
    response.headers.add('Access-Control-Allow-Headers', 'Content-Type,Authorization')
    response.headers.add('Access-Control-Expose-Headers', 'Content-Type,Content-Length,Authorization,X-Pagination')
    return response


if __name__ == '__main__':
    app.run(debug=True)

