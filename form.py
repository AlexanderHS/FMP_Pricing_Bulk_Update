from flask_wtf import FlaskForm
from wtforms import SubmitField, SelectField, DecimalField, StringField, FileField, validators
from wtforms.fields.simple import HiddenField
from wtforms.validators import DataRequired, AnyOf, ValidationError
from datetime import datetime, date
from wtforms.fields import DateField
from wtforms_components import DateRange
from dateutil.parser import parse

class UploadForm(FlaskForm):
    file = FileField()