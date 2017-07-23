$.fn.mvcgrid.lang = {
    Text: {
        Contains: 'Содержит',
        Equals: 'Равен',
        NotEquals: 'Не равен',
        StartsWith: 'Начинается с',
        EndsWith: 'Заканчивается на'
    },
    Number: {
        Equals: 'Равно',
        NotEquals: 'Не равно',
        LessThan: 'Меньше',
        GreaterThan: 'Больше',
        LessThanOrEqual: 'Меньше или равно',
        GreaterThanOrEqual: 'Больше или равно'
    },
    Date: {
        Equals: 'Равно',
        NotEquals: 'Не равно',
        LessThan: 'Меньше',
        GreaterThan: 'Больше',
        LessThanOrEqual: 'Меньше или равно',
        GreaterThanOrEqual: 'Больше или равно'
    },
    Boolean: {
        Yes: 'Да',
        No: 'Нет'
    },
    Filter: {
        Apply: '✔',
        Remove: '✘'
    },
    Operator: {
        Select: '',
        And: 'И',
        Or: 'ИЛИ'
    }
};

$(function () {
    var emoji = new EmojiConvertor();
    emoji.img_sets.apple.path = "https://raw.githubusercontent.com/iamcal/emoji-data/master/img-apple-64/";
    $('.achievement').each(function () { $(this).html(emoji.replace_unified($(this).html())) });
});