from flask_wtf import FlaskForm
from wtforms import SubmitField, SelectField, DecimalField, StringField, FileField, validators, BooleanField
from wtforms.fields.simple import HiddenField
from wtforms.validators import DataRequired, AnyOf, ValidationError, NoneOf
from flask_wtf.file import FileField, FileAllowed, FileRequired
from datetime import datetime, date
from wtforms.fields import DateField
from wtforms_components import DateRange
from dateutil.parser import parse

class UploadForm(FlaskForm):
    file = FileField("Choose File...", render_kw={"class": "btn btn-lg"}, validators=[DataRequired(), FileAllowed(['csv'], 'File must be csv.')])
    submit = SubmitField('Submit', render_kw={"class": "btn btn-lg btn-warning"})
    
class ConfirmForm(FlaskForm):
    confirm = BooleanField('Confirm?', validators=[DataRequired(), ])
    user = SelectField('Who is making this change?', validators=[DataRequired(), NoneOf('-', "Name of person making this change can't be blank.")])
    submit = SubmitField('Submit', render_kw={"class": "btn btn-lg btn-warning"})